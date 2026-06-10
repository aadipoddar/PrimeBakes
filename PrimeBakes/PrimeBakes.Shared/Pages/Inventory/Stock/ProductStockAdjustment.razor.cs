using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Inventory.Stock.Data;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Store.Product.Data;
using PrimeBakesLibrary.Store.Product.Models;

using Syncfusion.Blazor.Grids;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Inventory.Stock;

public partial class ProductStockAdjustment
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private DateTime _transactionDateTime = DateTime.Now;
	private string _transactionNo = string.Empty;

	private LocationModel _selectedLocation;
	private ProductLocationOverviewModel _selectedProduct = null;
	private ProductStockAdjustmentCartModel _selectedCart = new();

	private List<LocationModel> _locations = [];
	private List<ProductLocationOverviewModel> _products = [];
	private List<ProductStockAdjustmentCartModel> _cart = [];
	private List<ProductStockSummaryModel> _stockSummary = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomDatePicker _sfFirstFocus;
	private CustomAutoComplete<ProductLocationOverviewModel> _sfItemAutoComplete;
	private SfGrid<ProductStockAdjustmentCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory], true);
			await LoadData();
		}
		catch { await ResetPage(); }
	}

	private async Task LoadData()
	{
		_transactionDateTime = await CommonData.LoadCurrentDateTime();

		await LoadLocations();
		await LoadStock();
		await LoadItems();
		await LoadExistingCart();

		_isLoading = false;
		StateHasChanged();

		await SaveTransactionFile();

		if (_sfFirstFocus is not null)
			await _sfFirstFocus.FocusAsync();
	}

	private async Task LoadLocations()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);
		_locations = [.. _locations.OrderBy(s => s.Name)];

		_selectedLocation = _locations.FirstOrDefault(s => s.Id == 1);
		_transactionNo = await GenerateCodes.GenerateProductStockAdjustmentTransactionNo(_transactionDateTime, _selectedLocation.Id);
	}

	private async Task LoadStock() =>
		_stockSummary = await ProductStockData.LoadProductStockSummaryByDateLocationId(_transactionDateTime, _transactionDateTime, _selectedLocation.Id);

	private async Task LoadItems()
	{
		_products = await ProductLocationData.LoadProductLocationOverviewByProductLocation(LocationId: _selectedLocation.Id);
		_products = [.. _products.OrderBy(s => s.Name)];
	}

	private async Task LoadExistingCart()
	{
		try
		{
			_cart.Clear();

			if (await DataStorageService.LocalExists(StorageFileNames.ProductStockAdjustmentCartDataFileName))
				_cart = JsonSerializer.Deserialize<List<ProductStockAdjustmentCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.ProductStockAdjustmentCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
		}
	}
	#endregion

	#region Changed Events
	private async Task OnTransactionDateChanged(DateTime value)
	{
		_transactionDateTime = value;
		await LoadStock();
		await LoadItems();
		await SaveTransactionFile();
	}

	private async Task OnLocationChanged(LocationModel value)
	{
		_selectedLocation = value ?? _locations.FirstOrDefault(s => s.Id == 1);

		if (_selectedLocation is null || _selectedLocation.Id == 0)
			return;

		await LoadStock();
		await LoadItems();
		await SaveTransactionFile();
	}
	#endregion

	#region Cart
	private void OnItemChanged(ProductLocationOverviewModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedProduct = value;

		_selectedCart.Stock = _stockSummary.FirstOrDefault(s => s.ProductId == _selectedProduct.ProductId)?.ClosingStock ?? 0;
		_selectedCart.Quantity = _selectedCart.Stock;
		_selectedCart.Rate = _selectedProduct.Rate;

		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemQuantityChanged(decimal value)
	{
		_selectedCart.Quantity = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void UpdateSelectedItemFinancialDetails()
	{
		if (_selectedProduct is null)
			return;

		_selectedCart.ProductId = _selectedProduct.ProductId;
		_selectedCart.ProductName = _selectedProduct.Name;
		_selectedCart.Rate = _selectedProduct.Rate;
		_selectedCart.Stock = _stockSummary.FirstOrDefault(s => s.ProductId == _selectedProduct.ProductId)?.ClosingStock ?? 0;
		_selectedCart.Total = _selectedCart.Quantity * _selectedCart.Rate;

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedProduct is null || _selectedProduct.ProductId <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.ProductId == _selectedCart.ProductId);
		if (existingItem is not null)
		{
			existingItem.Quantity = _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
			existingItem.Total = _selectedCart.Total;
		}
		else
			_cart.Add(new()
			{
				ProductId = _selectedCart.ProductId,
				ProductName = _selectedCart.ProductName,
				Stock = _selectedCart.Stock,
				Quantity = _selectedCart.Quantity,
				Rate = _selectedCart.Rate,
				Total = _selectedCart.Total
			});

		_selectedProduct = null;
		_selectedCart = new();

		if (_sfItemAutoComplete is not null)
			await _sfItemAutoComplete.FocusAsync();

		await SaveTransactionFile();
	}

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		await EditCartItem(_sfCartGrid.SelectedRecords.First());
	}

	private async Task EditCartItem(ProductStockAdjustmentCartModel cartItem)
	{
		_selectedProduct = _products.FirstOrDefault(s => s.ProductId == cartItem.ProductId);

		if (_selectedProduct is null)
			return;

		_selectedCart = new()
		{
			ProductId = cartItem.ProductId,
			ProductName = cartItem.ProductName,
			Stock = _stockSummary.FirstOrDefault(s => s.ProductId == cartItem.ProductId)?.ClosingStock ?? 0,
			Quantity = cartItem.Quantity,
			Rate = cartItem.Rate,
			Total = cartItem.Total
		};

		UpdateSelectedItemFinancialDetails();
		await RemoveItemFromCart(cartItem);

		if (_sfItemAutoComplete is not null)
			await _sfItemAutoComplete.FocusAsync();
	}

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		await RemoveItemFromCart(_sfCartGrid.SelectedRecords.First());
	}

	private async Task RemoveItemFromCart(ProductStockAdjustmentCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails()
	{
		foreach (var item in _cart)
		{
			item.Stock = _stockSummary.FirstOrDefault(s => s.ProductId == item.ProductId)?.ClosingStock ?? 0;
			item.Total = item.Rate * item.Quantity;
		}

		_transactionNo = await GenerateCodes.GenerateProductStockAdjustmentTransactionNo(_transactionDateTime, _selectedLocation.Id);
	}

	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails();

			await DataStorageService.LocalSaveAsync(StorageFileNames.ProductStockAdjustmentCartDataFileName, JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Saving Transaction Data", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid.Refresh();

			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task SaveTransaction()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			await SaveTransactionFile();
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			await ProductStockData.SaveProductStockAdjustment(_transactionDateTime, _selectedLocation.Id, _cart, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

			await _toastNotification.ShowAsync("Save Transaction", "Transaction saved successfully.", ToastType.Success);
			await ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Saving Transaction", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Utilities
	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<ProductStockAdjustmentCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private async Task DeleteLocalFiles() =>
		await DataStorageService.LocalRemove(StorageFileNames.ProductStockAdjustmentCartDataFileName);

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		PageRefresh.Request();
	}
	#endregion
}

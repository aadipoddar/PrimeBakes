using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Inventory.Purchase.Data;
using PrimeBakesLibrary.Inventory.RawMaterial.Models;
using PrimeBakesLibrary.Inventory.Stock.Data;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.User;

using Syncfusion.Blazor.Grids;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Inventory.Stock;

public partial class RawMaterialStockAdjustmentPage
{
	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private DateTime _transactionDateTime = DateTime.Now;
	private string _transactionNo = string.Empty;

	private RawMaterialModel _selectedRawMaterial = null;
	private RawMaterialStockAdjustmentCartModel _selectedCart = new();

	private List<RawMaterialModel> _rawMaterials = [];
	private List<RawMaterialStockAdjustmentCartModel> _cart = [];
	private List<RawMaterialStockSummaryModel> _stockSummary = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomDatePicker _firstFocus;
	private CustomAutoComplete<RawMaterialModel> _itemAutoComplete;
	private SfGrid<RawMaterialStockAdjustmentCartModel> _sfCartGrid;

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
		_transactionNo = await GenerateCodes.GenerateRawMaterialStockAdjustmentTransactionNo(_transactionDateTime);

		await LoadStock();
		await LoadExistingCart();

		_isLoading = false;
		StateHasChanged();

		await SaveTransactionFile();

		if (_firstFocus is not null) await _firstFocus.FocusAsync();
	}

	private async Task LoadStock()
	{
		_stockSummary = await RawMaterialStockData.LoadRawMaterialStockSummaryByDate(_transactionDateTime, _transactionDateTime);

		_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _transactionDateTime);
		_rawMaterials = [.. _rawMaterials.OrderBy(s => s.Name)];
	}

	private async Task LoadExistingCart()
	{
		try
		{
			_cart.Clear();

			if (await DataStorageService.LocalExists(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName))
				_cart = JsonSerializer.Deserialize<List<RawMaterialStockAdjustmentCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName));
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
	}
	#endregion

	#region Cart
	private void OnItemChanged(RawMaterialModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedRawMaterial = value;

		_selectedCart.Stock = _stockSummary.FirstOrDefault(s => s.RawMaterialId == _selectedRawMaterial.Id)?.ClosingStock ?? 0;
		_selectedCart.Quantity = _selectedCart.Stock;
		_selectedCart.Rate = _selectedRawMaterial.Rate;

		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemQuantityChanged(decimal value)
	{
		_selectedCart.Quantity = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void UpdateSelectedItemFinancialDetails()
	{
		if (_selectedRawMaterial is null)
			return;

		_selectedCart.RawMaterialId = _selectedRawMaterial.Id;
		_selectedCart.RawMaterialName = _selectedRawMaterial.Name;
		_selectedCart.Rate = _selectedRawMaterial.Rate;
		_selectedCart.Stock = _stockSummary.FirstOrDefault(s => s.RawMaterialId == _selectedRawMaterial.Id)?.ClosingStock ?? 0;
		_selectedCart.Total = _selectedCart.Quantity * _selectedCart.Rate;

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedRawMaterial is null || _selectedRawMaterial.Id <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.RawMaterialId == _selectedCart.RawMaterialId);
		if (existingItem is not null)
		{
			existingItem.Quantity = _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
			existingItem.Total = _selectedCart.Total;
		}
		else
			_cart.Add(new()
			{
				RawMaterialId = _selectedCart.RawMaterialId,
				RawMaterialName = _selectedCart.RawMaterialName,
				Stock = _selectedCart.Stock,
				Quantity = _selectedCart.Quantity,
				Rate = _selectedCart.Rate,
				Total = _selectedCart.Total
			});

		_selectedRawMaterial = null;
		_selectedCart = new();

		if (_itemAutoComplete is not null)
			await _itemAutoComplete.FocusAsync();

		await SaveTransactionFile();
	}

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		await EditCartItem(_sfCartGrid.SelectedRecords.First());
	}

	private async Task EditCartItem(RawMaterialStockAdjustmentCartModel cartItem)
	{
		_selectedRawMaterial = _rawMaterials.FirstOrDefault(s => s.Id == cartItem.RawMaterialId);

		if (_selectedRawMaterial is null)
			return;

		_selectedCart = new()
		{
			RawMaterialId = cartItem.RawMaterialId,
			RawMaterialName = cartItem.RawMaterialName,
			Stock = _stockSummary.FirstOrDefault(s => s.RawMaterialId == cartItem.RawMaterialId)?.ClosingStock ?? 0,
			Quantity = cartItem.Quantity,
			Rate = cartItem.Rate,
			Total = cartItem.Total
		};

		UpdateSelectedItemFinancialDetails();
		await RemoveItemFromCart(cartItem);

		if (_itemAutoComplete is not null)
			await _itemAutoComplete.FocusAsync();
	}

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		await RemoveItemFromCart(_sfCartGrid.SelectedRecords.First());
	}

	private async Task RemoveItemFromCart(RawMaterialStockAdjustmentCartModel cartItem)
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
			item.Stock = _stockSummary.FirstOrDefault(s => s.RawMaterialId == item.RawMaterialId)?.ClosingStock ?? 0;
			item.Total = item.Rate * item.Quantity;
		}

		_transactionNo = await GenerateCodes.GenerateRawMaterialStockAdjustmentTransactionNo(_transactionDateTime);
	}

	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails();

			await DataStorageService.LocalSaveAsync(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName, JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Saving Transaction Data", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null) await _sfCartGrid.Refresh();

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

			await RawMaterialStockData.SaveRawMaterialStockAdjustment(_transactionDateTime, _cart, _user.Id, FormFactor.GetFormFactor() + FormFactor.GetPlatform());

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
	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<RawMaterialStockAdjustmentCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private async Task DeleteLocalFiles() =>
		await DataStorageService.LocalRemove(StorageFileNames.RawMaterialStockAdjustmentCartDataFileName);

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		PageRefresh.Request();
	}
	#endregion
}

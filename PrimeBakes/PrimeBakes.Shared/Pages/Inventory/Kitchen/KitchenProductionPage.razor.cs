using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Inventory.Kitchen.Data;
using PrimeBakesLibrary.Inventory.Kitchen.Models;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Store.Product.Models;

using Syncfusion.Blazor.Grids;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen;

public partial class KitchenProductionPage
{
	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany = new();
	private KitchenModel _selectedKitchen = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private ProductModel _selectedProduct = null;
	private KitchenProductionProductCartModel _selectedCart = new();
	private KitchenProductionModel _kitchenProduction = new();

	private List<CompanyModel> _companies = [];
	private List<KitchenModel> _kitchens = [];
	private List<ProductModel> _products = [];
	private List<KitchenProductionProductCartModel> _cart = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomAutoComplete<CompanyModel> _firstFocus;
	private CustomAutoComplete<ProductModel> _itemAutoComplete;
	private SfGrid<KitchenProductionProductCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory], true);
			await InitializePage();
		}
		catch { await ResetPage(); }
	}

	private async Task InitializePage()
	{
		await LoadData();
		await ResolveTransaction();
		await LoadSelections();
		await LoadItems();
		await ResolveCart();

		_isLoading = false;
		StateHasChanged();

		await SaveTransactionFile();

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_kitchens = await CommonData.LoadTableDataByStatus<KitchenModel>(InventoryNames.Kitchen);

		_companies = [.. _companies.OrderBy(s => s.Name)];
		_kitchens = [.. _kitchens.OrderBy(s => s.Name)];

		var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
		_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value) ?? _companies.FirstOrDefault();
		_selectedKitchen = _kitchens.FirstOrDefault();
	}

	private async Task ResolveTransaction()
	{
		try
		{
			if (await LoadExistingTransaction())
				return;

			if (await TryRestoreFromLocalStorage())
				return;

			await CreateNewTransaction();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Transaction Data", ex.Message, ToastType.Error);
			await ResetPage();
		}
	}

	private async Task<bool> LoadExistingTransaction()
	{
		if (!Id.HasValue)
			return false;

		_kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(InventoryNames.KitchenProduction, Id.Value);
		if (_kitchenProduction is null || _kitchenProduction.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			await ResetPage();
		}

		return true;
	}

	private async Task<bool> TryRestoreFromLocalStorage()
	{
		if (!await DataStorageService.LocalExists(StorageFileNames.KitchenProductionDataFileName))
			return false;

		try
		{
			_kitchenProduction = JsonSerializer.Deserialize<KitchenProductionModel>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenProductionDataFileName));
			return _kitchenProduction is not null;
		}
		catch
		{
			await DeleteLocalFiles();
			return false;
		}
	}

	private async Task CreateNewTransaction()
	{
		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var financialYear = await FinancialYearData.LoadFinancialYearByDateTime(currentDateTime);

		_kitchenProduction = new()
		{
			Id = 0,
			TransactionNo = string.Empty,
			CompanyId = _selectedCompany.Id,
			KitchenId = _selectedKitchen.Id,
			TransactionDateTime = currentDateTime,
			FinancialYearId = financialYear is null ? 0 : financialYear.Id,
			CreatedBy = _user.Id,
			TotalItems = 0,
			TotalQuantity = 0,
			TotalAmount = 0,
			Remarks = null,
			CreatedAt = DateTime.Now,
			CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
			Status = true,
			LastModifiedAt = null,
			LastModifiedBy = null,
			LastModifiedFromPlatform = null
		};

		await DeleteLocalFiles();
	}

	private async Task LoadSelections()
	{
		if (_kitchenProduction.CompanyId > 0)
			_selectedCompany = _companies.FirstOrDefault(s => s.Id == _kitchenProduction.CompanyId) ?? _companies.FirstOrDefault();
		else
			_selectedCompany = _companies.FirstOrDefault();

		if (_kitchenProduction.KitchenId > 0)
			_selectedKitchen = _kitchens.FirstOrDefault(s => s.Id == _kitchenProduction.KitchenId) ?? _kitchens.FirstOrDefault();
		else
			_selectedKitchen = _kitchens.FirstOrDefault();

		_kitchenProduction.CompanyId = _selectedCompany.Id;
		_kitchenProduction.KitchenId = _selectedKitchen.Id;

		_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _kitchenProduction.FinancialYearId);
	}

	private async Task LoadItems()
	{
		_products = await CommonData.LoadTableDataByStatus<ProductModel>(StoreNames.Product);
		_products = [.. _products.OrderBy(s => s.Name)];
	}

	private async Task ResolveCart()
	{
		try
		{
			_cart.Clear();

			if (await LoadExistingCart())
				return;

			if (await DataStorageService.LocalExists(StorageFileNames.KitchenProductionCartDataFileName))
				_cart = JsonSerializer.Deserialize<List<KitchenProductionProductCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenProductionCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await ResetPage();
		}
	}

	private async Task<bool> LoadExistingCart()
	{
		if (_kitchenProduction.Id <= 0)
			return false;

		var existingCart = await CommonData.LoadTableDataByMasterId<KitchenProductionDetailModel>(InventoryNames.KitchenProductionDetail, _kitchenProduction.Id);

		foreach (var item in existingCart)
		{
			if (_products.FirstOrDefault(s => s.Id == item.ProductId) is null)
			{
				var product = await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, item.ProductId);
				await _toastNotification.ShowAsync("Product Not Found", $"The product {product?.Name} (ID: {item.ProductId}) in the existing transaction cart was not found in the available products list. It may have been deleted or is inaccessible.", ToastType.Error);
				continue;
			}

			_cart.Add(new()
			{
				ProductId = item.ProductId,
				ProductName = _products.FirstOrDefault(s => s.Id == item.ProductId)?.Name ?? "",
				Quantity = item.Quantity,
				Rate = item.Rate,
				Total = item.Total,
				Remarks = item.Remarks
			});
		}

		return true;
	}
	#endregion

	#region Changed Events
	private async Task OnCompanyChanged(CompanyModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedCompany = value;
		await SaveTransactionFile();
	}

	private async Task OnKitchenChanged(KitchenModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedKitchen = value;
		await SaveTransactionFile();
		await LoadItems();
	}

	private async Task OnTransactionDateChanged(DateTime value)
	{
		_kitchenProduction.TransactionDateTime = value;
		await SaveTransactionFile();
		await LoadItems();
	}
	#endregion

	#region Cart
	private void OnItemChanged(ProductModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedProduct = value;

		_selectedCart.ProductId = _selectedProduct.Id;
		_selectedCart.ProductName = _selectedProduct.Name;
		_selectedCart.Quantity = 0;
		_selectedCart.Rate = _selectedProduct.Rate;

		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemQuantityChanged(decimal value)
	{
		_selectedCart.Quantity = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemRateChanged(decimal value)
	{
		_selectedCart.Rate = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void UpdateSelectedItemFinancialDetails()
	{
		if (_selectedProduct is null)
			return;

		if (_selectedCart.Quantity < 0)
			_selectedCart.Quantity = 1;

		_selectedCart.ProductId = _selectedProduct.Id;
		_selectedCart.ProductName = _selectedProduct.Name;
		_selectedCart.Total = _selectedCart.Rate * _selectedCart.Quantity;

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedProduct is null || _selectedProduct.Id <= 0 || _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0 || _selectedCart.Total < 0)
		{
			await _toastNotification.ShowAsync("Invalid Product Details", "Please ensure all product details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.ProductId == _selectedCart.ProductId);
		if (existingItem is not null)
		{
			existingItem.Quantity += _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
		}
		else
			_cart.Add(new()
			{
				ProductId = _selectedCart.ProductId,
				ProductName = _selectedCart.ProductName,
				Quantity = _selectedCart.Quantity,
				Rate = _selectedCart.Rate,
				Remarks = _selectedCart.Remarks
			});

		_selectedProduct = null;
		_selectedCart = new();

		await _itemAutoComplete.FocusAsync();
		await SaveTransactionFile();
	}

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await EditCartItem(selectedCartItem);
	}

	private async Task EditCartItem(KitchenProductionProductCartModel cartItem)
	{
		_selectedProduct = _products.FirstOrDefault(s => s.Id == cartItem.ProductId);

		if (_selectedProduct is null)
			return;

		_selectedCart = new()
		{
			ProductId = cartItem.ProductId,
			ProductName = cartItem.ProductName,
			Quantity = cartItem.Quantity,
			Rate = cartItem.Rate,
			Remarks = cartItem.Remarks
		};

		await _itemAutoComplete.FocusAsync();
		UpdateSelectedItemFinancialDetails();
		await RemoveItemFromCart(cartItem);
	}

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await RemoveItemFromCart(selectedCartItem);
	}

	private async Task RemoveItemFromCart(KitchenProductionProductCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails()
	{
		foreach (var item in _cart.ToList())
		{
			if (item.Quantity <= 0)
			{
				_cart.Remove(item);
				continue;
			}

			item.Total = item.Rate * item.Quantity;
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		}

		_kitchenProduction.CompanyId = _selectedCompany.Id;
		_kitchenProduction.KitchenId = _selectedKitchen.Id;
		_kitchenProduction.TotalItems = _cart.Count;
		_kitchenProduction.TotalQuantity = _cart.Sum(x => x.Quantity);
		_kitchenProduction.TotalAmount = _cart.Sum(x => x.Total);

		#region Financial Year
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_kitchenProduction.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_kitchenProduction.FinancialYearId = _selectedFinancialYear.Id;
		else
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);
		#endregion

		if (Id is null)
			_kitchenProduction.TransactionNo = await GenerateCodes.GenerateKitchenProductionTransactionNo(_kitchenProduction);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_kitchenProduction.Status = true;
		_kitchenProduction.TransactionDateTime = DateOnly.FromDateTime(_kitchenProduction.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
		_kitchenProduction.LastModifiedAt = currentDateTime;
		_kitchenProduction.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_kitchenProduction.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_kitchenProduction.CreatedBy = _user.Id;
		_kitchenProduction.LastModifiedBy = _user.Id;
	}

	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails();

			if (_cart.Count == 0 || _kitchenProduction.Id > 0)
			{
				await DeleteLocalFiles();
				return;
			}

			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenProductionDataFileName, JsonSerializer.Serialize(_kitchenProduction));
			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenProductionCartDataFileName, JsonSerializer.Serialize(_cart));
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

	private async Task SaveTransaction(bool savePDF = false, bool saveExcel = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			await SaveTransactionFile();
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			var items = KitchenProductionData.ConvertCartToDetails(_cart);
			_kitchenProduction.Id = await KitchenProductionData.SaveTransaction(_kitchenProduction, items);
			_kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(InventoryNames.KitchenProduction, _kitchenProduction.Id);

			if (savePDF) await ExportSelectedTransaction(false, true);
			if (saveExcel) await ExportSelectedTransaction(true, true);

			await _toastNotification.ShowAsync("Save Transaction", "Transaction saved successfully.", ToastType.Success);

			if (Id.HasValue && Id.Value > 0)
				await AuthenticationService.CloseWindowOrTab(FormFactor, JSRuntime);
			await ResetPage();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Saving Transaction", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}
	#endregion

	#region Exporting
	private async Task ExportSelectedTransaction(bool isExcel = false, bool force = false)
	{
		if (_kitchenProduction.Id <= 0 || (_isProcessing && !force))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_kitchenProduction.TransactionNo, !isExcel, isExcel, CodeType.KitchenProduction);
			await SaveAndViewService.SaveAndView(isExcel ? decodeTransactionNo.ExcelStream.fileName : decodeTransactionNo.PDFStream.fileName,
				isExcel ? decodeTransactionNo.ExcelStream.stream : decodeTransactionNo.PDFStream.stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Utilities
	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<KitchenProductionProductCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.KitchenProductionDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.KitchenProductionCartDataFileName);
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		PageRefresh.Request();
	}
	#endregion
}

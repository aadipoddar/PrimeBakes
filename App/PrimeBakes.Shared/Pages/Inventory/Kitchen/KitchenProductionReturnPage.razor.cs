using Microsoft.AspNetCore.Components;

using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Library.Inventory.Kitchen.Data;
using PrimeBakes.Library.Inventory.Stock.Data;
using PrimeBakes.Library.Operations.Settings;
using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.Stock;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen;

public partial class KitchenProductionReturnPage
{
	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private decimal _kitchenProductionReturnDiscountPercentage = 0;

	private CompanyModel _selectedCompany = new();
	private KitchenModel _selectedKitchen = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private ProductLocationOverviewModel _selectedProduct = null;
	private KitchenProductionReturnProductCartModel _selectedCart = new();
	private KitchenProductionReturnModel _kitchenProductionReturn = new();

	private List<ProductStockSummaryModel> _stockSummary = [];
	private List<CompanyModel> _companies = [];
	private List<KitchenModel> _kitchens = [];
	private List<ProductLocationOverviewModel> _products = [];
	private List<KitchenProductionReturnProductCartModel> _cart = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomAutoComplete<CompanyModel> _firstFocus;
	private CustomAutoComplete<ProductLocationOverviewModel> _itemAutoComplete;
	private SfGrid<KitchenProductionReturnProductCartModel> _sfCartGrid;

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

		await SaveTransactionFile(true);

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

		var discountSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.KitchenProductionReturnDiscountRate);
		_kitchenProductionReturnDiscountPercentage = decimal.Parse(discountSetting.Value);
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

		_kitchenProductionReturn = await CommonData.LoadTableDataById<KitchenProductionReturnModel>(InventoryNames.KitchenProductionReturn, Id.Value);
		if (_kitchenProductionReturn is null || _kitchenProductionReturn.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			await ResetPage();
		}

		return true;
	}

	private async Task<bool> TryRestoreFromLocalStorage()
	{
		if (!await DataStorageService.LocalExists(StorageFileNames.KitchenProductionReturnDataFileName))
			return false;

		try
		{
			_kitchenProductionReturn = JsonSerializer.Deserialize<KitchenProductionReturnModel>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenProductionReturnDataFileName));
			return _kitchenProductionReturn is not null;
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

		_kitchenProductionReturn = new()
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
		if (_kitchenProductionReturn.CompanyId > 0)
			_selectedCompany = _companies.FirstOrDefault(s => s.Id == _kitchenProductionReturn.CompanyId) ?? _companies.FirstOrDefault();
		else
			_selectedCompany = _companies.FirstOrDefault();

		if (_kitchenProductionReturn.KitchenId > 0)
			_selectedKitchen = _kitchens.FirstOrDefault(s => s.Id == _kitchenProductionReturn.KitchenId) ?? _kitchens.FirstOrDefault();
		else
			_selectedKitchen = _kitchens.FirstOrDefault();

		_kitchenProductionReturn.CompanyId = _selectedCompany.Id;
		_kitchenProductionReturn.KitchenId = _selectedKitchen.Id;

		_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _kitchenProductionReturn.FinancialYearId);
	}

	private async Task LoadItems()
	{
		_products = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(null, 1, DateOnly.FromDateTime(_kitchenProductionReturn.TransactionDateTime));
		_products = [.. _products.OrderBy(s => s.Name)];

		_stockSummary = await ProductStockData.LoadProductStockSummaryByDateLocationId(_kitchenProductionReturn.TransactionDateTime, _kitchenProductionReturn.TransactionDateTime, 1);
	}

	private async Task ResolveCart()
	{
		try
		{
			_cart.Clear();

			if (await LoadExistingCart())
				return;

			if (await DataStorageService.LocalExists(StorageFileNames.KitchenProductionReturnCartDataFileName))
				_cart = JsonSerializer.Deserialize<List<KitchenProductionReturnProductCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenProductionReturnCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await ResetPage();
		}
	}

	private async Task<bool> LoadExistingCart()
	{
		if (_kitchenProductionReturn.Id <= 0)
			return false;

		var existingCart = await CommonData.LoadTableDataByMasterId<KitchenProductionReturnDetailModel>(InventoryNames.KitchenProductionReturnDetail, _kitchenProductionReturn.Id);

		foreach (var item in existingCart)
		{
			if (_products.FirstOrDefault(s => s.ProductId == item.ProductId) is null)
			{
				var product = await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, item.ProductId);
				await _toastNotification.ShowAsync("Product Not Found", $"The product {product?.Name} (ID: {item.ProductId}) in the existing transaction cart was not found in the available products list. It may have been deleted or is inaccessible.", ToastType.Error);
				continue;
			}

			_cart.Add(new()
			{
				ProductId = item.ProductId,
				ProductName = _products.FirstOrDefault(s => s.ProductId == item.ProductId)?.Name ?? "",
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
		_kitchenProductionReturn.TransactionDateTime = value;
		await LoadItems();
	}
	#endregion

	#region Cart
	private void OnItemChanged(ProductLocationOverviewModel value)
	{
		if (value is null || value.ProductId == 0)
			return;

		_selectedProduct = value;

		_selectedCart.ProductId = _selectedProduct.ProductId;
		_selectedCart.ProductName = _selectedProduct.Name;
		_selectedCart.Quantity = 0;
		_selectedCart.Rate = _selectedProduct.Rate * (100 / (100 + _kitchenProductionReturnDiscountPercentage));

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

	private void OnItemTotalChanged(decimal value)
	{
		_selectedCart.Rate = value / (_selectedCart.Quantity > 0 ? _selectedCart.Quantity : 1);
		UpdateSelectedItemFinancialDetails();
	}

	private void UpdateSelectedItemFinancialDetails()
	{
		if (_selectedProduct is null)
			return;

		if (_selectedCart.Quantity < 0)
			_selectedCart.Quantity = 1;

		_selectedCart.ProductId = _selectedProduct.ProductId;
		_selectedCart.ProductName = _selectedProduct.Name;
		_selectedCart.Total = _selectedCart.Rate * _selectedCart.Quantity;

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedProduct is null || _selectedProduct.ProductId <= 0 || _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0 || _selectedCart.Total < 0)
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

	private async Task EditCartItem(KitchenProductionReturnProductCartModel cartItem)
	{
		_selectedProduct = _products.FirstOrDefault(s => s.ProductId == cartItem.ProductId);

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

	private async Task RemoveItemFromCart(KitchenProductionReturnProductCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private void UpdateFinancialDetails()
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

		_cart = [.. _cart.OrderBy(s => s.ProductName)];

		_kitchenProductionReturn.CompanyId = _selectedCompany.Id;
		_kitchenProductionReturn.KitchenId = _selectedKitchen.Id;
		_kitchenProductionReturn.TotalItems = _cart.Count;
		_kitchenProductionReturn.TotalQuantity = _cart.Sum(x => x.Quantity);
		_kitchenProductionReturn.TotalAmount = _cart.Sum(x => x.Total);
	}

	private async Task PrepareSave()
	{
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_kitchenProductionReturn.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_kitchenProductionReturn.FinancialYearId = _selectedFinancialYear.Id;
		else
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);

		if (Id is null)
			_kitchenProductionReturn.TransactionNo = await GenerateCodes.GenerateKitchenProductionReturnTransactionNo(_kitchenProductionReturn);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_kitchenProductionReturn.Status = true;
		_kitchenProductionReturn.TransactionDateTime = DateOnly.FromDateTime(_kitchenProductionReturn.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
		_kitchenProductionReturn.LastModifiedAt = currentDateTime;
		_kitchenProductionReturn.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_kitchenProductionReturn.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_kitchenProductionReturn.CreatedBy = _user.Id;
		_kitchenProductionReturn.LastModifiedBy = _user.Id;
	}

	private async Task SaveTransactionFile(bool prepareSave = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			UpdateFinancialDetails();
			if (prepareSave) await PrepareSave();

			if (_cart.Count == 0 || _kitchenProductionReturn.Id > 0)
			{
				await DeleteLocalFiles();
				return;
			}

			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenProductionReturnDataFileName, JsonSerializer.Serialize(_kitchenProductionReturn));
			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenProductionReturnCartDataFileName, JsonSerializer.Serialize(_cart));
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

	private async Task SaveTransaction(bool savePDF = false, bool saveExcel = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			await SaveTransactionFile(true);
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			var items = KitchenProductionReturnData.ConvertCartToDetails(_cart);
			_kitchenProductionReturn.Id = await KitchenProductionReturnData.SaveTransaction(_kitchenProductionReturn, items);
			_kitchenProductionReturn = await CommonData.LoadTableDataById<KitchenProductionReturnModel>(InventoryNames.KitchenProductionReturn, _kitchenProductionReturn.Id);

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
		if (_kitchenProductionReturn.Id <= 0 || (_isProcessing && !force))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_kitchenProductionReturn.TransactionNo, !isExcel, isExcel, CodeType.KitchenProductionReturn);
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
	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<KitchenProductionReturnProductCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.KitchenProductionReturnDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.KitchenProductionReturnCartDataFileName);
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		PageRefresh.Request();
	}
	#endregion
}

using Microsoft.AspNetCore.Components;

using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Operations.Settings;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Store.Order.Data;
using PrimeBakes.Library.Store.Order.Models;
using PrimeBakes.Library.Store.Product.Data;
using PrimeBakes.Library.Store.Product.Models;
using PrimeBakes.Library.Store.Sale.Models;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Store.Order;

public partial class OrderPage
{
	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany = new();
	private CompanyModel _mainCompany = new();
	private LocationModel _selectedLocation = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private ProductLocationOverviewModel _selectedProduct = null;
	private OrderItemCartModel _selectedCart = new();
	private OrderModel _order = new();
	private SaleModel _sale = new();

	private List<CompanyModel> _companies = [];
	private List<LocationModel> _locations = [];
	private List<ProductLocationOverviewModel> _products = [];
	private List<OrderItemCartModel> _cart = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomAutoComplete<CompanyModel> _firstFocus;
	private CustomAutoComplete<ProductLocationOverviewModel> _itemAutoComplete;
	private SfGrid<OrderItemCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store]);
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

		if (_firstFocus is not null) await _firstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_locations = await CommonData.LoadTableDataByStatus<LocationModel>(OperationNames.Location);
		_locations.RemoveAll(s => s.Id == 1);
		_locations = [.. _locations.OrderBy(s => s.Name)];

		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_companies = [.. _companies.OrderBy(s => s.Name)];

		var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
		_mainCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value) ?? _companies.FirstOrDefault();
		_selectedCompany = _mainCompany;

		_selectedLocation = _user.LocationId > 1
			? _locations.FirstOrDefault(s => s.Id == _user.LocationId)
			: _locations.FirstOrDefault();
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

		_order = await CommonData.LoadTableDataById<OrderModel>(StoreNames.Order, Id.Value);
		if (_order is null || _order.Id == 0 || _user.LocationId > 1)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			await ResetPage();
		}

		return true;
	}

	private async Task<bool> TryRestoreFromLocalStorage()
	{
		if (!await DataStorageService.LocalExists(StorageFileNames.OrderDataFileName))
			return false;

		try
		{
			_order = JsonSerializer.Deserialize<OrderModel>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderDataFileName));
			return _order is not null;
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

		_order = new()
		{
			Id = 0,
			TransactionNo = string.Empty,
			CompanyId = _mainCompany.Id,
			LocationId = _user.LocationId > 1 ? _user.LocationId : _locations.FirstOrDefault()?.Id ?? 0,
			SaleId = null,
			TransactionDateTime = currentDateTime,
			FinancialYearId = financialYear is null ? 0 : financialYear.Id,
			TotalItems = 0,
			TotalQuantity = 0,
			Remarks = null,
			CreatedBy = _user.Id,
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
		_selectedLocation = _user.LocationId == 1
			? _locations.FirstOrDefault(s => s.Id == _order.LocationId) ?? _locations.FirstOrDefault()
			: _locations.FirstOrDefault(s => s.Id == _user.LocationId);
		_order.LocationId = _selectedLocation?.Id ?? 0;

		_selectedCompany = _user.LocationId == 1 && _order.CompanyId > 0
			? _companies.FirstOrDefault(s => s.Id == _order.CompanyId) ?? _mainCompany
			: _mainCompany;
		_order.CompanyId = _selectedCompany?.Id ?? 0;

		if (_order.SaleId is not null && _order.SaleId > 0)
			_sale = await CommonData.LoadTableDataById<SaleModel>(StoreNames.Sale, _order.SaleId.Value);

		_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _order.FinancialYearId);
	}

	private async Task LoadItems()
	{
		var orderDate = DateOnly.FromDateTime(_order.TransactionDateTime);
		var mainLocationProducts = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(null, 1, orderDate);
		var orderLocationProducts = await ProductLocationData.LoadProductLocationOverviewByProductLocationDate(null, _order.LocationId, orderDate);

		_products = [.. mainLocationProducts.Where(x => orderLocationProducts.Any(y => y.ProductId == x.ProductId)).OrderBy(s => s.Name)];
	}

	private async Task ResolveCart()
	{
		try
		{
			_cart.Clear();

			if (await LoadExistingCart())
				return;

			if (await DataStorageService.LocalExists(StorageFileNames.OrderCartDataFileName))
				_cart = JsonSerializer.Deserialize<List<OrderItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.OrderCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await ResetPage();
		}
	}

	private async Task<bool> LoadExistingCart()
	{
		if (_order.Id <= 0)
			return false;

		var existingCart = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(StoreNames.OrderDetail, _order.Id);

		foreach (var item in existingCart)
		{
			var product = _products.FirstOrDefault(s => s.ProductId == item.ProductId);
			if (product is null)
			{
				var missing = await CommonData.LoadTableDataById<ProductModel>(StoreNames.Product, item.ProductId);
				await _toastNotification.ShowAsync("Item Not Found", $"The item {missing?.Name} (ID: {item.ProductId}) in the existing transaction cart was not found in the available items list. It may have been deleted or is inaccessible.", ToastType.Error);
				continue;
			}

			_cart.Add(new()
			{
				ItemCategoryId = product.ProductCategoryId,
				ItemId = item.ProductId,
				ItemName = product.Name,
				Quantity = item.Quantity,
				Remarks = item.Remarks
			});
		}

		return true;
	}
	#endregion

	#region Changed Events
	private async Task OnLocationChanged(LocationModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedLocation = value;
		_order.LocationId = value.Id;
		await SaveTransactionFile();
		await LoadItems();
	}

	private async Task OnCompanyChanged(CompanyModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedCompany = value;
		_order.CompanyId = value.Id;
		await SaveTransactionFile();
	}
	#endregion

	#region Cart
	private void OnItemChanged(ProductLocationOverviewModel value)
	{
		if (value is null || value.ProductId <= 0)
			return;

		_selectedProduct = value;

		_selectedCart.ItemCategoryId = value.ProductCategoryId;
		_selectedCart.ItemId = value.ProductId;
		_selectedCart.ItemName = value.Name;
		_selectedCart.Quantity = 0;

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

		if (_selectedCart.Quantity < 0)
			_selectedCart.Quantity = 1;

		_selectedCart.ItemId = _selectedProduct.ProductId;
		_selectedCart.ItemName = _selectedProduct.Name;

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedProduct is null || _selectedProduct.ProductId <= 0 || _selectedCart.Quantity <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.ItemId == _selectedCart.ItemId);
		if (existingItem is not null)
			existingItem.Quantity += _selectedCart.Quantity;
		else
			_cart.Add(new()
			{
				ItemCategoryId = _selectedCart.ItemCategoryId,
				ItemId = _selectedCart.ItemId,
				ItemName = _selectedCart.ItemName,
				Quantity = _selectedCart.Quantity,
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

	private async Task EditCartItem(OrderItemCartModel cartItem)
	{
		_selectedProduct = _products.FirstOrDefault(s => s.ProductId == cartItem.ItemId);

		if (_selectedProduct is null)
			return;

		_selectedCart = new()
		{
			ItemCategoryId = cartItem.ItemCategoryId,
			ItemId = cartItem.ItemId,
			ItemName = cartItem.ItemName,
			Quantity = cartItem.Quantity,
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

	private async Task RemoveItemFromCart(OrderItemCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private void UpdateFinancialDetails()
	{
		if (_user.LocationId > 1)
			_selectedLocation = _locations.FirstOrDefault(s => s.Id == _user.LocationId);

		foreach (var item in _cart.ToList())
		{
			if (item.Quantity <= 0)
			{
				_cart.Remove(item);
				continue;
			}

			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		}

		_order.TotalItems = _cart.Count;
		_order.TotalQuantity = _cart.Sum(x => x.Quantity);

		_order.CompanyId = _selectedCompany?.Id ?? 0;
		_order.LocationId = _selectedLocation?.Id ?? 0;
	}

	private async Task PrepareSave()
	{
		if (_user.LocationId != 1)
		{
			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value);
			_order.TransactionDateTime = await CommonData.LoadCurrentDateTime();
		}

		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_order.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_order.FinancialYearId = _selectedFinancialYear.Id;
		else
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);

		if (Id is null)
			_order.TransactionNo = await GenerateCodes.GenerateOrderTransactionNo(_order);

		if (_order.SaleId is not null && _order.SaleId > 0)
			_sale = await CommonData.LoadTableDataById<SaleModel>(StoreNames.Sale, _order.SaleId.Value);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_order.Status = true;
		_order.TransactionDateTime = DateOnly.FromDateTime(_order.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
		_order.LastModifiedAt = currentDateTime;
		_order.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_order.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_order.CreatedBy = _user.Id;
		_order.LastModifiedBy = _user.Id;
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

			if (_cart.Count == 0 || _order.Id > 0)
			{
				await DeleteLocalFiles();
				return;
			}

			await DataStorageService.LocalSaveAsync(StorageFileNames.OrderDataFileName, JsonSerializer.Serialize(_order));
			await DataStorageService.LocalSaveAsync(StorageFileNames.OrderCartDataFileName, JsonSerializer.Serialize(_cart));
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

			var orderDetails = OrderData.ConvertCartToDetails(_cart);
			_order.Id = await OrderData.SaveTransaction(_order, orderDetails);
			_order = await CommonData.LoadTableDataById<OrderModel>(StoreNames.Order, _order.Id);

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
		if (_order.Id <= 0 || (_isProcessing && !force))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_order.TransactionNo, !isExcel, isExcel, CodeType.Order);
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

	private async Task ViewSelectedSale()
	{
		if (_order.SaleId is null or <= 0)
		{
			await _toastNotification.ShowAsync("No Sale Linked", "There is no sale linked to this order to view.", ToastType.Error);
			return;
		}

		var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_sale.TransactionNo, false, false, CodeType.Sale);
		await AuthenticationService.NavigateToRoute(decodeTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
	}
	#endregion

	#region Utilities
	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<OrderItemCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.OrderDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.OrderCartDataFileName);
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		PageRefresh.Request();
	}
	#endregion
}

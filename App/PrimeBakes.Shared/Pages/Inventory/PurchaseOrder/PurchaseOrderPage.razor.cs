using Microsoft.AspNetCore.Components;

using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Inventory.PurchaseOrder;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.PurchaseOrder;
using PrimeBakes.Models.Inventory.RawMaterial;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Inventory.PurchaseOrder;

public partial class PurchaseOrderPage
{
	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany = new();
	private LedgerModel _selectedParty = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private RawMaterialModel _selectedRawMaterial = null;
	private PurchaseOrderItemCartModel _selectedCart = new();
	private PurchaseOrderModel _purchaseOrder = new();
	private PurchaseModel _purchase = new();

	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<RawMaterialModel> _rawMaterials = [];
	private List<PurchaseOrderItemCartModel> _cart = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomAutoComplete<CompanyModel> _firstFocus;
	private CustomAutoComplete<RawMaterialModel> _itemAutoComplete;
	private SfGrid<PurchaseOrderItemCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	private DateTime? _expectedDeliveryDate;

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

		if (_firstFocus is not null) await _firstFocus.FocusAsync();
	}

	private async Task LoadData()
	{
		_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
		_parties = await CommonData.LoadTableDataByStatus<LedgerModel>(AccountNames.Ledger);

		_companies = [.. _companies.OrderBy(s => s.Name)];
		_parties = [.. _parties.OrderBy(s => s.Name)];

		var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
		_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value) ?? _companies.FirstOrDefault();
		_selectedParty = _parties.FirstOrDefault();
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

		_purchaseOrder = await CommonData.LoadTableDataById<PurchaseOrderModel>(InventoryNames.PurchaseOrder, Id.Value);
		if (_purchaseOrder is null || _purchaseOrder.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			await ResetPage();
		}

		return true;
	}

	private async Task<bool> TryRestoreFromLocalStorage()
	{
		if (!await DataStorageService.LocalExists(StorageFileNames.PurchaseOrderDataFileName))
			return false;

		try
		{
			_purchaseOrder = JsonSerializer.Deserialize<PurchaseOrderModel>(await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseOrderDataFileName));
			return _purchaseOrder is not null;
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

		_purchaseOrder = new()
		{
			Id = 0,
			TransactionNo = string.Empty,
			CompanyId = _selectedCompany?.Id ?? 0,
			PartyId = _selectedParty?.Id ?? 0,
			PurchaseId = null,
			TransactionDateTime = currentDateTime,
			ExpectedDeliveryDate = null,
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
		_selectedCompany = _companies.FirstOrDefault(s => s.Id == _purchaseOrder.CompanyId) ?? _companies.FirstOrDefault();
		_purchaseOrder.CompanyId = _selectedCompany?.Id ?? 0;

		_selectedParty = _parties.FirstOrDefault(s => s.Id == _purchaseOrder.PartyId) ?? _parties.FirstOrDefault();
		_purchaseOrder.PartyId = _selectedParty?.Id ?? 0;

		_expectedDeliveryDate = _purchaseOrder.ExpectedDeliveryDate?.ToDateTime(TimeOnly.MinValue);

		if (_purchaseOrder.PurchaseId is not null && _purchaseOrder.PurchaseId > 0)
			_purchase = await CommonData.LoadTableDataById<PurchaseModel>(InventoryNames.Purchase, _purchaseOrder.PurchaseId.Value);

		_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _purchaseOrder.FinancialYearId);
	}

	private async Task LoadItems()
	{
		_rawMaterials = await CommonData.LoadTableDataByStatus<RawMaterialModel>(InventoryNames.RawMaterial);
		_rawMaterials = [.. _rawMaterials.OrderBy(s => s.Name)];
	}

	private async Task ResolveCart()
	{
		try
		{
			_cart.Clear();

			if (await LoadExistingCart())
				return;

			if (await DataStorageService.LocalExists(StorageFileNames.PurchaseOrderCartDataFileName))
				_cart = JsonSerializer.Deserialize<List<PurchaseOrderItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseOrderCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await ResetPage();
		}
	}

	private async Task<bool> LoadExistingCart()
	{
		if (_purchaseOrder.Id <= 0)
			return false;

		var existingCart = await CommonData.LoadTableDataByMasterId<PurchaseOrderDetailModel>(InventoryNames.PurchaseOrderDetail, _purchaseOrder.Id);

		foreach (var item in existingCart)
		{
			var rawMaterial = _rawMaterials.FirstOrDefault(s => s.Id == item.RawMaterialId);
			if (rawMaterial is null)
			{
				var missing = await CommonData.LoadTableDataById<RawMaterialModel>(InventoryNames.RawMaterial, item.RawMaterialId);
				await _toastNotification.ShowAsync("Item Not Found", $"The item {missing?.Name} (ID: {item.RawMaterialId}) in the existing transaction cart was not found in the available items list. It may have been deleted or is inaccessible.", ToastType.Error);
				continue;
			}

			_cart.Add(new()
			{
				ItemCategoryId = rawMaterial.RawMaterialCategoryId,
				ItemId = item.RawMaterialId,
				ItemName = rawMaterial.Name,
				Quantity = item.Quantity,
				UnitOfMeasurement = item.UnitOfMeasurement,
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
		_purchaseOrder.CompanyId = value.Id;
		await SaveTransactionFile();
	}

	private async Task OnPartyChanged(LedgerModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedParty = value;
		_purchaseOrder.PartyId = value.Id;
		await SaveTransactionFile();
	}

	private async Task OnTransactionDateChanged(DateTime value)
	{
		_purchaseOrder.TransactionDateTime = value;
		await SaveTransactionFile();
	}

	private async Task OnExpectedDeliveryDateChanged(DateTime? value)
	{
		_expectedDeliveryDate = value;
		_purchaseOrder.ExpectedDeliveryDate = value is null ? null : DateOnly.FromDateTime(value.Value);
		await SaveTransactionFile();
	}
	#endregion

	#region Cart
	private void OnItemChanged(RawMaterialModel value)
	{
		if (value is null || value.Id <= 0)
			return;

		_selectedRawMaterial = value;

		_selectedCart.ItemCategoryId = value.RawMaterialCategoryId;
		_selectedCart.ItemId = value.Id;
		_selectedCart.ItemName = value.Name;
		_selectedCart.UnitOfMeasurement = value.UnitOfMeasurement;
		_selectedCart.Quantity = 0;

		UpdateSelectedItemDetails();
	}

	private void OnItemQuantityChanged(decimal value)
	{
		_selectedCart.Quantity = value;
		UpdateSelectedItemDetails();
	}

	private void UpdateSelectedItemDetails()
	{
		if (_selectedRawMaterial is null)
			return;

		if (_selectedCart.Quantity < 0)
			_selectedCart.Quantity = 1;

		_selectedCart.ItemId = _selectedRawMaterial.Id;
		_selectedCart.ItemName = _selectedRawMaterial.Name;
		_selectedCart.UnitOfMeasurement = _selectedRawMaterial.UnitOfMeasurement;

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedRawMaterial is null || _selectedRawMaterial.Id <= 0 || _selectedCart.Quantity <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		UpdateSelectedItemDetails();

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
				UnitOfMeasurement = _selectedCart.UnitOfMeasurement,
				Remarks = _selectedCart.Remarks
			});

		_selectedRawMaterial = null;
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

	private async Task EditCartItem(PurchaseOrderItemCartModel cartItem)
	{
		_selectedRawMaterial = _rawMaterials.FirstOrDefault(s => s.Id == cartItem.ItemId);

		if (_selectedRawMaterial is null)
			return;

		_selectedCart = new()
		{
			ItemCategoryId = cartItem.ItemCategoryId,
			ItemId = cartItem.ItemId,
			ItemName = cartItem.ItemName,
			Quantity = cartItem.Quantity,
			UnitOfMeasurement = cartItem.UnitOfMeasurement,
			Remarks = cartItem.Remarks
		};

		await _itemAutoComplete.FocusAsync();
		UpdateSelectedItemDetails();
		await RemoveItemFromCart(cartItem);
	}

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await RemoveItemFromCart(selectedCartItem);
	}

	private async Task RemoveItemFromCart(PurchaseOrderItemCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private void UpdateTransactionDetails()
	{
		foreach (var item in _cart.ToList())
		{
			if (item.Quantity <= 0)
			{
				_cart.Remove(item);
				continue;
			}

			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		}

		_cart = [.. _cart.OrderBy(s => s.ItemName)];

		_purchaseOrder.TotalItems = _cart.Count;
		_purchaseOrder.TotalQuantity = _cart.Sum(x => x.Quantity);

		_purchaseOrder.CompanyId = _selectedCompany?.Id ?? 0;
		_purchaseOrder.PartyId = _selectedParty?.Id ?? 0;
	}

	private async Task PrepareSave()
	{
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_purchaseOrder.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_purchaseOrder.FinancialYearId = _selectedFinancialYear.Id;
		else
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);

		if (Id is null)
			_purchaseOrder.TransactionNo = await GenerateCodes.GeneratePurchaseOrderTransactionNo(_purchaseOrder);

		if (_purchaseOrder.PurchaseId is not null && _purchaseOrder.PurchaseId > 0)
			_purchase = await CommonData.LoadTableDataById<PurchaseModel>(InventoryNames.Purchase, _purchaseOrder.PurchaseId.Value);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_purchaseOrder.Status = true;
		_purchaseOrder.TransactionDateTime = DateOnly.FromDateTime(_purchaseOrder.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
		_purchaseOrder.LastModifiedAt = currentDateTime;
		_purchaseOrder.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_purchaseOrder.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_purchaseOrder.CreatedBy = _user.Id;
		_purchaseOrder.LastModifiedBy = _user.Id;
	}

	private async Task SaveTransactionFile(bool prepareSave = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			UpdateTransactionDetails();
			if (prepareSave) await PrepareSave();

			if (_cart.Count == 0 || _purchaseOrder.Id > 0)
			{
				await DeleteLocalFiles();
				return;
			}

			await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseOrderDataFileName, JsonSerializer.Serialize(_purchaseOrder));
			await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseOrderCartDataFileName, JsonSerializer.Serialize(_cart));
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

			var purchaseOrderDetails = _cart.ConvertCartToDetails();
			_purchaseOrder.Id = await PurchaseOrderData.SaveTransaction(_purchaseOrder, purchaseOrderDetails);
			_purchaseOrder = await CommonData.LoadTableDataById<PurchaseOrderModel>(InventoryNames.PurchaseOrder, _purchaseOrder.Id);

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
		if (_purchaseOrder.Id <= 0 || (_isProcessing && !force))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_purchaseOrder.TransactionNo, !isExcel, isExcel, CodeType.PurchaseOrder);
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

	private async Task ViewSelectedPurchase()
	{
		if (_purchaseOrder.PurchaseId is null or <= 0)
		{
			await _toastNotification.ShowAsync("No Purchase Linked", "There is no purchase linked to this purchase order to view.", ToastType.Error);
			return;
		}

		var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_purchase.TransactionNo, false, false, CodeType.Purchase);
		await AuthenticationService.NavigateToRoute(decodeTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
	}
	#endregion

	#region Utilities
	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<PurchaseOrderItemCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.PurchaseOrderDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.PurchaseOrderCartDataFileName);
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		PageRefresh.Request();
	}
	#endregion
}

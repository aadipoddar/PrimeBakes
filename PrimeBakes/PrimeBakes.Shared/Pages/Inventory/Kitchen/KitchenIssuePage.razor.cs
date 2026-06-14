using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Inventory.Kitchen.Data;
using PrimeBakesLibrary.Inventory.Kitchen.Models;
using PrimeBakesLibrary.Inventory.Purchase.Data;
using PrimeBakesLibrary.Inventory.RawMaterial.Models;
using PrimeBakesLibrary.Inventory.Stock.Data;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;

using Syncfusion.Blazor.Grids;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen;

public partial class KitchenIssuePage
{
	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany = new();
	private KitchenModel _selectedKitchen = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private RawMaterialModel _selectedRawMaterial = null;
	private KitchenIssueItemCartModel _selectedCart = new();
	private KitchenIssueModel _kitchenIssue = new();

	private List<RawMaterialStockSummaryModel> _stockSummary = [];
	private List<CompanyModel> _companies = [];
	private List<KitchenModel> _kitchens = [];
	private List<RawMaterialModel> _rawMaterials = [];
	private List<KitchenIssueItemCartModel> _cart = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomAutoComplete<CompanyModel> _firstFocus;
	private CustomAutoComplete<RawMaterialModel> _itemAutoComplete;
	private SfGrid<KitchenIssueItemCartModel> _sfCartGrid;

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

		_kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(InventoryNames.KitchenIssue, Id.Value);
		if (_kitchenIssue is null || _kitchenIssue.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			await ResetPage();
		}

		return true;
	}

	private async Task<bool> TryRestoreFromLocalStorage()
	{
		if (!await DataStorageService.LocalExists(StorageFileNames.KitchenIssueDataFileName))
			return false;

		try
		{
			_kitchenIssue = JsonSerializer.Deserialize<KitchenIssueModel>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenIssueDataFileName));
			return _kitchenIssue is not null;
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

		_kitchenIssue = new()
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
		if (_kitchenIssue.CompanyId > 0)
			_selectedCompany = _companies.FirstOrDefault(s => s.Id == _kitchenIssue.CompanyId) ?? _companies.FirstOrDefault();
		else
			_selectedCompany = _companies.FirstOrDefault();

		if (_kitchenIssue.KitchenId > 0)
			_selectedKitchen = _kitchens.FirstOrDefault(s => s.Id == _kitchenIssue.KitchenId) ?? _kitchens.FirstOrDefault();
		else
			_selectedKitchen = _kitchens.FirstOrDefault();

		_kitchenIssue.CompanyId = _selectedCompany.Id;
		_kitchenIssue.KitchenId = _selectedKitchen.Id;

		_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _kitchenIssue.FinancialYearId);
	}

	private async Task LoadItems()
	{
		_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(0, _kitchenIssue.TransactionDateTime);
		_rawMaterials = [.. _rawMaterials.OrderBy(s => s.Name)];

		_stockSummary = await RawMaterialStockData.LoadRawMaterialStockSummaryByDate(_kitchenIssue.TransactionDateTime, _kitchenIssue.TransactionDateTime);
	}

	private async Task ResolveCart()
	{
		try
		{
			_cart.Clear();

			if (await LoadExistingCart())
				return;

			if (await DataStorageService.LocalExists(StorageFileNames.KitchenIssueCartDataFileName))
				_cart = JsonSerializer.Deserialize<List<KitchenIssueItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.KitchenIssueCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await ResetPage();
		}
	}

	private async Task<bool> LoadExistingCart()
	{
		if (_kitchenIssue.Id <= 0)
			return false;

		var existingCart = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(InventoryNames.KitchenIssueDetail, _kitchenIssue.Id);

		foreach (var item in existingCart)
		{
			if (_rawMaterials.FirstOrDefault(s => s.Id == item.RawMaterialId) is null)
			{
				var rawMaterial = await CommonData.LoadTableDataById<RawMaterialModel>(InventoryNames.RawMaterial, item.RawMaterialId);
				await _toastNotification.ShowAsync("Item Not Found", $"The item {rawMaterial?.Name} (ID: {item.RawMaterialId}) in the existing transaction cart was not found in the available items list. It may have been deleted or is inaccessible.", ToastType.Error);
				continue;
			}

			_cart.Add(new()
			{
				ItemId = item.RawMaterialId,
				ItemName = _rawMaterials.FirstOrDefault(s => s.Id == item.RawMaterialId)?.Name ?? "",
				Quantity = item.Quantity,
				UnitOfMeasurement = item.UnitOfMeasurement,
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
		_kitchenIssue.TransactionDateTime = value;
		await SaveTransactionFile();
		await LoadItems();
	}
	#endregion

	#region Cart
	private void OnItemChanged(RawMaterialModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedRawMaterial = value;

		_selectedCart.ItemId = _selectedRawMaterial.Id;
		_selectedCart.ItemName = _selectedRawMaterial.Name;
		_selectedCart.Quantity = 0;
		_selectedCart.UnitOfMeasurement = _selectedRawMaterial.UnitOfMeasurement;
		_selectedCart.Rate = _selectedRawMaterial.Rate;

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
		if (_selectedRawMaterial is null)
			return;

		if (_selectedCart.Quantity < 0)
			_selectedCart.Quantity = 1;

		if (string.IsNullOrWhiteSpace(_selectedCart.UnitOfMeasurement))
			_selectedCart.UnitOfMeasurement = _selectedRawMaterial.UnitOfMeasurement;

		_selectedCart.ItemId = _selectedRawMaterial.Id;
		_selectedCart.ItemName = _selectedRawMaterial.Name;
		_selectedCart.Total = _selectedCart.Rate * _selectedCart.Quantity;

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedRawMaterial is null || _selectedRawMaterial.Id <= 0 || _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0 || _selectedCart.Total < 0 || string.IsNullOrWhiteSpace(_selectedCart.UnitOfMeasurement))
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.ItemId == _selectedCart.ItemId);
		if (existingItem is not null)
		{
			existingItem.Quantity += _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
		}
		else
			_cart.Add(new()
			{
				ItemId = _selectedCart.ItemId,
				ItemName = _selectedCart.ItemName,
				Quantity = _selectedCart.Quantity,
				UnitOfMeasurement = _selectedCart.UnitOfMeasurement,
				Rate = _selectedCart.Rate,
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

	private async Task EditCartItem(KitchenIssueItemCartModel cartItem)
	{
		_selectedRawMaterial = _rawMaterials.FirstOrDefault(s => s.Id == cartItem.ItemId);

		if (_selectedRawMaterial is null)
			return;

		_selectedCart = new()
		{
			ItemId = cartItem.ItemId,
			ItemName = cartItem.ItemName,
			Quantity = cartItem.Quantity,
			UnitOfMeasurement = cartItem.UnitOfMeasurement,
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

	private async Task RemoveItemFromCart(KitchenIssueItemCartModel cartItem)
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

		_kitchenIssue.CompanyId = _selectedCompany.Id;
		_kitchenIssue.KitchenId = _selectedKitchen.Id;
		_kitchenIssue.TotalItems = _cart.Count;
		_kitchenIssue.TotalQuantity = _cart.Sum(x => x.Quantity);
		_kitchenIssue.TotalAmount = _cart.Sum(x => x.Total);

		#region Financial Year
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_kitchenIssue.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_kitchenIssue.FinancialYearId = _selectedFinancialYear.Id;
		else
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);
		#endregion

		if (Id is null)
			_kitchenIssue.TransactionNo = await GenerateCodes.GenerateKitchenIssueTransactionNo(_kitchenIssue);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_kitchenIssue.Status = true;
		_kitchenIssue.TransactionDateTime = DateOnly.FromDateTime(_kitchenIssue.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
		_kitchenIssue.LastModifiedAt = currentDateTime;
		_kitchenIssue.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_kitchenIssue.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_kitchenIssue.CreatedBy = _user.Id;
		_kitchenIssue.LastModifiedBy = _user.Id;
	}

	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails();

			if (_cart.Count == 0 || _kitchenIssue.Id > 0)
			{
				await DeleteLocalFiles();
				return;
			}

			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenIssueDataFileName, JsonSerializer.Serialize(_kitchenIssue));
			await DataStorageService.LocalSaveAsync(StorageFileNames.KitchenIssueCartDataFileName, JsonSerializer.Serialize(_cart));
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

			var items = KitchenIssueData.ConvertCartToDetails(_cart);
			_kitchenIssue.Id = await KitchenIssueData.SaveTransaction(_kitchenIssue, items);
			_kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(InventoryNames.KitchenIssue, _kitchenIssue.Id);

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
		if (_kitchenIssue.Id <= 0 || (_isProcessing && !force))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_kitchenIssue.TransactionNo, !isExcel, isExcel, CodeType.KitchenIssue);
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
	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<KitchenIssueItemCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.KitchenIssueDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.KitchenIssueCartDataFileName);
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		PageRefresh.Request();
	}
	#endregion
}

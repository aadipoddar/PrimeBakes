using Microsoft.AspNetCore.Components;

using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.DataAccess;
using PrimeBakes.Library.Inventory.Purchase.Data;
using PrimeBakes.Library.Inventory.Purchase.Models;
using PrimeBakes.Library.Inventory.RawMaterial.Models;
using PrimeBakes.Library.Operations.Settings;
using PrimeBakes.Library.Operations.User;
using PrimeBakes.Library.Store.Product.Models;
using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using Syncfusion.Blazor.Grids;
using Syncfusion.Blazor.Inputs;

using System.Text.Json;

namespace PrimeBakes.Shared.Pages.Inventory.Purchase;

public partial class PurchasePage
{
	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _isUploadDialogVisible = false;

	private CompanyModel _selectedCompany = new();
	private LedgerModel _selectedParty = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private RawMaterialModel _selectedRawMaterial = null;
	private PurchaseItemCartModel _selectedCart = new();
	private PurchaseModel _purchase = new();

	private List<CompanyModel> _companies = [];
	private List<LedgerModel> _parties = [];
	private List<RawMaterialModel> _rawMaterials = [];
	private List<TaxModel> _taxes = [];
	private List<PurchaseItemCartModel> _cart = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomAutoComplete<CompanyModel> _firstFocus;
	private CustomAutoComplete<RawMaterialModel> _itemAutoComplete;
	private CustomNumericField<decimal> _otherChargesPercentField;
	private SfGrid<PurchaseItemCartModel> _sfCartGrid;
	private SfUploader _sfDocumentUploader;

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

		_purchase = await CommonData.LoadTableDataById<PurchaseModel>(InventoryNames.Purchase, Id.Value);
		if (_purchase is null || _purchase.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			await ResetPage();
		}

		return true;
	}

	private async Task<bool> TryRestoreFromLocalStorage()
	{
		if (!await DataStorageService.LocalExists(StorageFileNames.PurchaseDataFileName))
			return false;

		try
		{
			_purchase = JsonSerializer.Deserialize<PurchaseModel>(await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseDataFileName));
			return _purchase is not null;
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

		_purchase = new()
		{
			Id = 0,
			TransactionNo = string.Empty,
			ChallanNo = null,
			CompanyId = _selectedCompany.Id,
			PartyId = _selectedParty.Id,
			TransactionDateTime = currentDateTime,
			FinancialYearId = financialYear is null ? 0 : financialYear.Id,
			CreatedBy = _user.Id,
			BaseTotal = 0,
			TotalQuantity = 0,
			DocumentUrl = null,
			TotalItems = 0,
			ItemDiscountAmount = 0,
			TotalAfterItemDiscount = 0,
			TotalInclusiveTaxAmount = 0,
			TotalExtraTaxAmount = 0,
			TotalAfterTax = 0,
			CashDiscountPercent = 0,
			CashDiscountAmount = 0,
			OtherChargesPercent = 0,
			OtherChargesAmount = 0,
			RoundOffAmount = 0,
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
		if (_purchase.CompanyId > 0)
			_selectedCompany = _companies.FirstOrDefault(s => s.Id == _purchase.CompanyId) ?? _companies.FirstOrDefault();
		else
			_selectedCompany = _companies.FirstOrDefault();

		if (_purchase.PartyId > 0)
			_selectedParty = _parties.FirstOrDefault(s => s.Id == _purchase.PartyId) ?? _parties.FirstOrDefault();
		else
			_selectedParty = _parties.FirstOrDefault();

		_purchase.CompanyId = _selectedCompany.Id;
		_purchase.PartyId = _selectedParty.Id;

		_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _purchase.FinancialYearId);
	}

	private async Task LoadItems()
	{
		_rawMaterials = await PurchaseData.LoadRawMaterialByPartyPurchaseDateTime(_purchase.PartyId, _purchase.TransactionDateTime);
		_taxes = await CommonData.LoadTableData<TaxModel>(StoreNames.Tax);

		_rawMaterials = [.. _rawMaterials.OrderBy(s => s.Name)];
	}

	private async Task ResolveCart()
	{
		try
		{
			_cart.Clear();

			if (await LoadExistingCart())
				return;

			if (await DataStorageService.LocalExists(StorageFileNames.PurchaseCartDataFileName))
				_cart = JsonSerializer.Deserialize<List<PurchaseItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.PurchaseCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await ResetPage();
		}
	}

	private async Task<bool> LoadExistingCart()
	{
		if (_purchase.Id <= 0)
			return false;

		var existingCart = await CommonData.LoadTableDataByMasterId<PurchaseDetailModel>(InventoryNames.PurchaseDetail, _purchase.Id);

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
				BaseTotal = item.BaseTotal,
				DiscountPercent = item.DiscountPercent,
				DiscountAmount = item.DiscountAmount,
				AfterDiscount = item.AfterDiscount,
				CGSTPercent = item.CGSTPercent,
				CGSTAmount = item.CGSTAmount,
				SGSTPercent = item.SGSTPercent,
				SGSTAmount = item.SGSTAmount,
				IGSTPercent = item.IGSTPercent,
				IGSTAmount = item.IGSTAmount,
				TotalTaxAmount = item.TotalTaxAmount,
				Total = item.Total,
				InclusiveTax = item.InclusiveTax,
				NetRate = item.NetRate,
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

	private async Task OnPartyChanged(LedgerModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedParty = value;
		await SaveTransactionFile();
		await LoadItems();
	}

	private async Task OnTransactionDateChanged(DateTime value)
	{
		_purchase.TransactionDateTime = value;
		await LoadItems();
	}

	private async Task OnOtherChargesPercentChanged(decimal value)
	{
		_purchase.OtherChargesPercent = value;
		await SaveTransactionFile();
	}

	private async Task OnCashDiscountPercentChanged(decimal value)
	{
		_purchase.CashDiscountPercent = value;
		await SaveTransactionFile();
	}

	private async Task OnRoundOffAmountChanged(decimal value)
	{
		_purchase.RoundOffAmount = value;
		await SaveTransactionFile(false, true);
	}
	#endregion

	#region Cart
	private void OnItemChanged(RawMaterialModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedRawMaterial = value;

		var isSameState = _selectedParty.StateUTId == _selectedCompany.StateUTId;
		var tax = _taxes.FirstOrDefault(s => s.Id == _selectedRawMaterial.TaxId);

		_selectedCart.ItemId = _selectedRawMaterial.Id;
		_selectedCart.ItemName = _selectedRawMaterial.Name;
		_selectedCart.Quantity = 0;
		_selectedCart.UnitOfMeasurement = _selectedRawMaterial.UnitOfMeasurement;
		_selectedCart.Rate = _selectedRawMaterial.Rate;
		_selectedCart.DiscountPercent = 0;
		_selectedCart.CGSTPercent = tax.CGST;
		_selectedCart.SGSTPercent = isSameState ? tax.SGST : 0;
		_selectedCart.IGSTPercent = isSameState ? 0 : tax.IGST;
		_selectedCart.InclusiveTax = tax.Inclusive;

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

	private void OnItemDiscountPercentChanged(decimal value)
	{
		_selectedCart.DiscountPercent = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemCGSTPercentChanged(decimal value)
	{
		_selectedCart.CGSTPercent = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemSGSTPercentChanged(decimal value)
	{
		_selectedCart.SGSTPercent = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemIGSTPercentChanged(decimal value)
	{
		_selectedCart.IGSTPercent = value;
		UpdateSelectedItemFinancialDetails();
	}

	private void OnItemInclusiveTaxChanged(bool value)
	{
		_selectedCart.InclusiveTax = value;
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
		_selectedCart.BaseTotal = _selectedCart.Rate * _selectedCart.Quantity;
		_selectedCart.DiscountAmount = _selectedCart.BaseTotal * (_selectedCart.DiscountPercent / 100);
		_selectedCart.AfterDiscount = _selectedCart.BaseTotal - _selectedCart.DiscountAmount;

		if (_selectedCart.InclusiveTax)
		{
			_selectedCart.CGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.CGSTPercent / (100 + _selectedCart.CGSTPercent));
			_selectedCart.SGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.SGSTPercent / (100 + _selectedCart.SGSTPercent));
			_selectedCart.IGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.IGSTPercent / (100 + _selectedCart.IGSTPercent));
			_selectedCart.TotalTaxAmount = _selectedCart.CGSTAmount + _selectedCart.SGSTAmount + _selectedCart.IGSTAmount;
			_selectedCart.Total = _selectedCart.AfterDiscount;
		}
		else
		{
			_selectedCart.CGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.CGSTPercent / 100);
			_selectedCart.SGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.SGSTPercent / 100);
			_selectedCart.IGSTAmount = _selectedCart.AfterDiscount * (_selectedCart.IGSTPercent / 100);
			_selectedCart.TotalTaxAmount = _selectedCart.CGSTAmount + _selectedCart.SGSTAmount + _selectedCart.IGSTAmount;
			_selectedCart.Total = _selectedCart.AfterDiscount + _selectedCart.TotalTaxAmount;
		}

		StateHasChanged();
	}

	private async Task AddItemToCart()
	{
		if (_selectedRawMaterial is null || _selectedRawMaterial.Id <= 0 || _selectedCart.Quantity <= 0 || _selectedCart.Rate < 0 || _selectedCart.DiscountPercent < 0 || _selectedCart.CGSTPercent < 0 || _selectedCart.SGSTPercent < 0 || _selectedCart.IGSTPercent < 0 || _selectedCart.Total < 0 || string.IsNullOrWhiteSpace(_selectedCart.UnitOfMeasurement))
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		// Validate that all three taxes cannot be applied together
		int taxCount = 0;
		if (_selectedCart.CGSTPercent > 0) taxCount++;
		if (_selectedCart.SGSTPercent > 0) taxCount++;
		if (_selectedCart.IGSTPercent > 0) taxCount++;

		if (taxCount == 3)
		{
			await _toastNotification.ShowAsync("Invalid Tax Configuration", "All three taxes (CGST, SGST, IGST) cannot be applied together. Use either CGST+SGST or IGST only.", ToastType.Error);
			return;
		}

		UpdateSelectedItemFinancialDetails();

		var existingItem = _cart.FirstOrDefault(s => s.ItemId == _selectedCart.ItemId);
		if (existingItem is not null)
		{
			existingItem.Quantity += _selectedCart.Quantity;
			existingItem.Rate = _selectedCart.Rate;
			existingItem.DiscountPercent = _selectedCart.DiscountPercent;
			existingItem.CGSTPercent = _selectedCart.CGSTPercent;
			existingItem.SGSTPercent = _selectedCart.SGSTPercent;
			existingItem.IGSTPercent = _selectedCart.IGSTPercent;
		}
		else
			_cart.Add(new()
			{
				ItemId = _selectedCart.ItemId,
				ItemName = _selectedCart.ItemName,
				Quantity = _selectedCart.Quantity,
				UnitOfMeasurement = _selectedCart.UnitOfMeasurement,
				Rate = _selectedCart.Rate,
				DiscountPercent = _selectedCart.DiscountPercent,
				CGSTPercent = _selectedCart.CGSTPercent,
				SGSTPercent = _selectedCart.SGSTPercent,
				IGSTPercent = _selectedCart.IGSTPercent,
				InclusiveTax = _selectedCart.InclusiveTax,
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

	private async Task EditCartItem(PurchaseItemCartModel cartItem)
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
			DiscountPercent = cartItem.DiscountPercent,
			CGSTPercent = cartItem.CGSTPercent,
			SGSTPercent = cartItem.SGSTPercent,
			IGSTPercent = cartItem.IGSTPercent,
			InclusiveTax = cartItem.InclusiveTax,
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

	private async Task RemoveItemFromCart(PurchaseItemCartModel cartItem)
	{
		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}
	#endregion

	#region Saving
	private void UpdateFinancialDetails(bool customRoundOff = false)
	{
		foreach (var item in _cart.ToList())
		{
			if (item.Quantity <= 0)
			{
				_cart.Remove(item);
				continue;
			}

			item.BaseTotal = item.Rate * item.Quantity;
			item.DiscountAmount = item.BaseTotal * (item.DiscountPercent / 100);
			item.AfterDiscount = item.BaseTotal - item.DiscountAmount;

			if (item.InclusiveTax)
			{
				item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / (100 + item.CGSTPercent));
				item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / (100 + item.SGSTPercent));
				item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / (100 + item.IGSTPercent));
				item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
				item.Total = item.AfterDiscount;
			}
			else
			{
				item.CGSTAmount = item.AfterDiscount * (item.CGSTPercent / 100);
				item.SGSTAmount = item.AfterDiscount * (item.SGSTPercent / 100);
				item.IGSTAmount = item.AfterDiscount * (item.IGSTPercent / 100);
				item.TotalTaxAmount = item.CGSTAmount + item.SGSTAmount + item.IGSTAmount;
				item.Total = item.AfterDiscount + item.TotalTaxAmount;
			}

			var perUnitCost = item.Total / item.Quantity;
			var withOtherCharges = perUnitCost * (1 + _purchase.OtherChargesPercent / 100);
			item.NetRate = withOtherCharges * (1 - _purchase.CashDiscountPercent / 100);

			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
		}

		_purchase.CompanyId = _selectedCompany.Id;
		_purchase.PartyId = _selectedParty.Id;
		_purchase.TotalItems = _cart.Count;
		_purchase.TotalQuantity = _cart.Sum(x => x.Quantity);
		_purchase.BaseTotal = _cart.Sum(x => x.BaseTotal);
		_purchase.ItemDiscountAmount = _cart.Sum(x => x.DiscountAmount);
		_purchase.TotalAfterItemDiscount = _cart.Sum(x => x.AfterDiscount);
		_purchase.TotalInclusiveTaxAmount = _cart.Where(x => x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_purchase.TotalExtraTaxAmount = _cart.Where(x => !x.InclusiveTax).Sum(x => x.TotalTaxAmount);
		_purchase.TotalAfterTax = _cart.Sum(x => x.Total);

		_purchase.OtherChargesAmount = _purchase.TotalAfterTax * _purchase.OtherChargesPercent / 100;
		var totalAfterOtherCharges = _purchase.TotalAfterTax + _purchase.OtherChargesAmount;

		_purchase.CashDiscountAmount = totalAfterOtherCharges * _purchase.CashDiscountPercent / 100;
		var totalAfterCashDiscount = totalAfterOtherCharges - _purchase.CashDiscountAmount;

		if (!customRoundOff) _purchase.RoundOffAmount = Math.Round(totalAfterCashDiscount) - totalAfterCashDiscount;
		_purchase.TotalAmount = totalAfterCashDiscount + _purchase.RoundOffAmount;
	}

	private async Task PrepareSave()
	{
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_purchase.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_purchase.FinancialYearId = _selectedFinancialYear.Id;
		else
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);

		if (Id is null)
			_purchase.TransactionNo = await GenerateCodes.GeneratePurchaseTransactionNo(_purchase);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_purchase.Status = true;
		_purchase.TransactionDateTime = DateOnly.FromDateTime(_purchase.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
		_purchase.LastModifiedAt = currentDateTime;
		_purchase.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_purchase.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_purchase.CreatedBy = _user.Id;
		_purchase.LastModifiedBy = _user.Id;
	}

	private async Task SaveTransactionFile(bool prepareSave = false, bool customRoundOff = false)
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			UpdateFinancialDetails(customRoundOff);
			if (prepareSave) await PrepareSave();

			if (_cart.Count == 0 || _purchase.Id > 0)
			{
				await DeleteLocalFiles();
				return;
			}

			await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseDataFileName, JsonSerializer.Serialize(_purchase));
			await DataStorageService.LocalSaveAsync(StorageFileNames.PurchaseCartDataFileName, JsonSerializer.Serialize(_cart));
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
			await SaveTransactionFile(true, true);
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			var items = PurchaseData.ConvertCartToDetails(_cart);
			_purchase.Id = await PurchaseData.SaveTransaction(_purchase, items);
			_purchase = await CommonData.LoadTableDataById<PurchaseModel>(InventoryNames.Purchase, _purchase.Id);

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
		if (_purchase.Id <= 0 || (_isProcessing && !force))
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_purchase.TransactionNo, !isExcel, isExcel, CodeType.Purchase);
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

	#region Uploading Document
	private void UploadDocument()
	{
		if (_isProcessing)
			return;

		_isUploadDialogVisible = true;
		StateHasChanged();
	}

	private void CloseUploadDialog()
	{
		_isUploadDialogVisible = false;
		StateHasChanged();
	}

	private async Task OnUploaderFileChange(UploadChangeEventArgs args)
	{
		try
		{
			if (args.Files is null || args.Files.Count != 1)
				return;

			if (!string.IsNullOrWhiteSpace(_purchase.DocumentUrl))
				await RemoveExistingDocument();

			await using var file = args.Files[0].File.OpenReadStream(maxAllowedSize: 52428800); // 50 MB
			var fileName = $"{Guid.NewGuid()}_{args.Files[0].File.Name}";
			_purchase.DocumentUrl = await BlobStorageAccess.UploadFileToBlobStorage(file, fileName, BlobStorageContainers.purchase);

			await SaveTransactionFile();
			await _toastNotification.ShowAsync("Document Uploaded Successfully", "The document has been uploaded and linked to the transaction.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Uploading Document", ex.Message, ToastType.Error);
		}
	}

	private async Task OnRemoveFile(RemovingEventArgs args) =>
		await RemoveExistingDocument();

	private async Task RemoveExistingDocument()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_purchase.DocumentUrl))
				return;

			var fileName = _purchase.DocumentUrl.Split('/').Last();
			await BlobStorageAccess.DeleteFileFromBlobStorage(fileName, BlobStorageContainers.purchase);
			_purchase.DocumentUrl = null;

			await SaveTransactionFile();
			await _toastNotification.ShowAsync("Document Removed", "The uploaded document has been removed successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Removing Document", ex.Message, ToastType.Error);
		}
	}

	private async Task DownloadExistingDocument()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(_purchase.DocumentUrl))
				return;

			var (fileStream, contentType) = await BlobStorageAccess.DownloadFileFromBlobStorage(_purchase.DocumentUrl, BlobStorageContainers.purchase);
			var fileName = _purchase.DocumentUrl.Split('/').Last();
			await SaveAndViewService.SaveAndView(fileName, fileStream);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Downloading Document", ex.Message, ToastType.Error);
		}
	}
	#endregion

	#region Utilities
	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<PurchaseItemCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.PurchaseDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.PurchaseCartDataFileName);
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		PageRefresh.Request();
	}
	#endregion
}

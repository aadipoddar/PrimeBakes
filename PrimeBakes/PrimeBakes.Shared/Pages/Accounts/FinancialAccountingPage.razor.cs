using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Components.Dialog;
using PrimeBakes.Shared.Components.Input;

using PrimeBakesLibrary.Accounts.FinancialAccounting.Data;
using PrimeBakesLibrary.Accounts.FinancialAccounting.Models;
using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;

using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Accounts;

public partial class FinancialAccountingPage
{
	[Parameter] public int? Id { get; set; }

	private UserModel _user;
	private bool _isLoading = true;
	private bool _isProcessing = false;
	private bool _showReferenceInstrument = false;

	private CompanyModel _selectedCompany = new();
	private VoucherModel _selectedVoucher = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private LedgerModel? _selectedLedger = null;
	private FinancialAccountingLedgerOverviewModel? _selectedAccountingLedger = null;
	private FinancialAccountingLedgerCartModel _selectedCart = new();
	private FinancialAccountingModel _accounting = new();

	private List<CompanyModel> _companies = [];
	private List<VoucherModel> _vouchers = [];
	private List<LedgerModel> _ledgers = [];
	private List<FinancialAccountingLedgerOverviewModel> _accountingLedgers = [];
	private List<FinancialAccountingLedgerCartModel> _cart = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private CustomAutoComplete<CompanyModel> _firstFocus;
	private CustomAutoComplete<LedgerModel> _sfLedgerAutoComplete;
	private SfGrid<FinancialAccountingLedgerCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Accounts], true);
			await InitializePage();
		}
		catch { await ResetPage(); }
	}

	private async Task InitializePage()
	{
		await LoadCompanies();
		await LoadVouchers();
		await ResolveTransaction();
		await LoadSelections();
		await LoadLedgers();
		await LoadCart();

		_isLoading = false;
		StateHasChanged();

		await SaveTransactionFile();

		if (_firstFocus is not null)
			await _firstFocus.FocusAsync();
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

		_accounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, Id.Value);
		if (_accounting is null || _accounting.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			await ResetPage();
		}

		return true;
	}

	private async Task<bool> TryRestoreFromLocalStorage()
	{
		if (!await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingDataFileName))
			return false;

		try
		{
			_accounting = System.Text.Json.JsonSerializer.Deserialize<FinancialAccountingModel>(await DataStorageService.LocalGetAsync(StorageFileNames.FinancialAccountingDataFileName));
			if (_accounting is null)
				return false;

			return true;
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

		_accounting = new()
		{
			Id = 0,
			TransactionNo = string.Empty,
			CompanyId = _selectedCompany.Id,
			TransactionDateTime = currentDateTime,
			ReferenceId = null,
			ReferenceNo = null,
			VoucherId = _selectedVoucher.Id,
			FinancialYearId = financialYear is null ? 0 : financialYear.Id,
			TotalDebitAmount = 0,
			TotalCreditAmount = 0,
			TotalDebitLedgers = 0,
			TotalCreditLedgers = 0,
			Remarks = string.Empty,
			CreatedBy = _user.Id,
			CreatedAt = DateTime.Now,
			CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform(),
			Status = true,
			LastModifiedAt = null,
			LastModifiedBy = null,
			LastModifiedFromPlatform = null
		};

		var lastTransaction = await CommonData.LoadLastTableData<FinancialAccountingModel>(AccountNames.FinancialAccounting);
		if (lastTransaction is not null)
			_accounting.TransactionDateTime = lastTransaction.TransactionDateTime;

		await DeleteLocalFiles();
	}

	private async Task LoadSelections()
	{
		if (_accounting.CompanyId > 0)
			_selectedCompany = _companies.FirstOrDefault(s => s.Id == _accounting.CompanyId) ?? _companies.FirstOrDefault() ?? new();
		else
		{
			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value) ?? _companies.FirstOrDefault() ?? new();
		}
		_accounting.CompanyId = _selectedCompany.Id;

		if (_accounting.VoucherId > 0)
			_selectedVoucher = _vouchers.FirstOrDefault(s => s.Id == _accounting.VoucherId) ?? _vouchers.FirstOrDefault() ?? new();
		else
			_selectedVoucher = _vouchers.FirstOrDefault() ?? new();
		_accounting.VoucherId = _selectedVoucher.Id;

		if (_accounting.FinancialYearId > 0)
			_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(AccountNames.FinancialYear, _accounting.FinancialYearId);

		if (_selectedFinancialYear is null || _selectedFinancialYear.Id <= 0)
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_accounting.TransactionDateTime);

		if (_selectedFinancialYear is not null)
			_accounting.FinancialYearId = _selectedFinancialYear.Id;
	}

	private async Task LoadCart()
	{
		try
		{
			_cart.Clear();

			if (_accounting.Id > 0)
			{
				var existingCart = await CommonData.LoadTableDataByMasterId<FinancialAccountingLedgerModel>(AccountNames.FinancialAccountingLedger, _accounting.Id);

				foreach (var item in existingCart)
				{
					if (_ledgers.FirstOrDefault(s => s.Id == item.LedgerId) is null)
					{
						var ledger = await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, item.LedgerId);
						await _toastNotification.ShowAsync("Ledger Not Found", $"The ledger {ledger?.Name} (ID: {item.LedgerId}) in the existing transaction cart was not found in the available ledgers list. It may have been deleted or is inaccessible.", ToastType.Error);
						continue;
					}

					_cart.Add(new()
					{
						LedgerId = item.LedgerId,
						LedgerName = _ledgers.First(s => s.Id == item.LedgerId).Name,
						Credit = item.Credit,
						Debit = item.Debit,
						ReferenceId = item.ReferenceId,
						ReferenceNo = item.ReferenceNo,
						ReferenceType = item.ReferenceType,
						Remarks = item.Remarks
					});
				}
			}

			else if (await DataStorageService.LocalExists(StorageFileNames.FinancialAccountingCartDataFileName))
				_cart = System.Text.Json.JsonSerializer.Deserialize<List<FinancialAccountingLedgerCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.FinancialAccountingCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
			await ResetPage();
		}
	}

	private async Task LoadCompanies()
	{
		try
		{
			_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(AccountNames.Company);
			_companies = [.. _companies.OrderBy(s => s.Name)];

			var mainCompanyId = await SettingsData.LoadSettingsByKey(SettingsKeys.PrimaryCompanyLinkingId);
			_selectedCompany = _companies.FirstOrDefault(s => s.Id.ToString() == mainCompanyId.Value) ?? throw new Exception("Main Company Not Found");
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Companies", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadVouchers()
	{
		try
		{
			_vouchers = await CommonData.LoadTableDataByStatus<VoucherModel>(AccountNames.Voucher);
			_vouchers = [.. _vouchers.OrderBy(s => s.Name)];

			var defaultSelectedVoucherId = await SettingsData.LoadSettingsByKey(SettingsKeys.DefaultSelectedVoucherId);
			_selectedVoucher = _vouchers.FirstOrDefault(v => v.Id.ToString() == defaultSelectedVoucherId.Value) ?? _vouchers.FirstOrDefault();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Vouchers", ex.Message, ToastType.Error);
		}
	}

	private async Task LoadLedgers()
	{
		try
		{
			_ledgers = await CommonData.LoadTableDataByStatus<LedgerModel>(AccountNames.Ledger);
			_ledgers = [.. _ledgers.OrderBy(s => s.Name)];
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Ledgers", ex.Message, ToastType.Error);
		}
	}
	#endregion

	#region Changed Events
	private async Task OnCompanyChanged(CompanyModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedCompany = value;
		_accounting.CompanyId = value.Id;

		await SaveTransactionFile();
	}

	private async Task OnVoucherChanged(VoucherModel value)
	{
		if (value is null || value.Id == 0)
			return;

		_selectedVoucher = value;
		_accounting.VoucherId = value.Id;

		await SaveTransactionFile();
	}
	#endregion

	#region Cart
	private void OnItemChanged(LedgerModel value)
	{
		if (value is null || value.Id == 0)
		{
			_selectedLedger = null;
			_selectedCart = new();
			return;
		}

		_selectedLedger = value;

		_selectedCart.LedgerId = _selectedLedger.Id;
		_selectedCart.LedgerName = _selectedLedger.Name;
		_selectedCart.Credit = null;
		_selectedCart.Debit = null;
		_selectedCart.ReferenceId = null;
		_selectedCart.ReferenceNo = null;
		_selectedCart.ReferenceType = null;
	}

	private void OnReferenceChanged(FinancialAccountingLedgerOverviewModel value)
	{
		if (value is null)
		{
			_selectedAccountingLedger = null;
			_selectedCart.ReferenceNo = null;
			_selectedCart.ReferenceId = null;
			_selectedCart.ReferenceType = null;
			return;
		}

		_selectedAccountingLedger = value;
		_selectedCart.ReferenceNo = value.LedgerReferenceNo;
		_selectedCart.ReferenceId = value.LedgerReferenceId;
		_selectedCart.ReferenceType = value.LedgerReferenceType;

		if ((_selectedAccountingLedger.Debit ?? 0) > (_selectedAccountingLedger.Credit ?? 0))
			_selectedCart.Credit = (_selectedAccountingLedger.Debit ?? 0) - (_selectedAccountingLedger.Credit ?? 0);
		else if ((_selectedAccountingLedger.Credit ?? 0) > (_selectedAccountingLedger.Debit ?? 0))
			_selectedCart.Debit = (_selectedAccountingLedger.Credit ?? 0) - (_selectedAccountingLedger.Debit ?? 0);
	}

	private async Task AddItemToCart()
	{
		if (_selectedLedger is null ||
			((_selectedCart.Debit ?? 0) <= 0 && (_selectedCart.Credit ?? 0) <= 0) ||
			((_selectedCart.Debit ?? 0) > 0 && (_selectedCart.Credit ?? 0) > 0) ||
			(_selectedCart.Debit ?? 0) < 0 || (_selectedCart.Credit ?? 0) < 0)
		{
			await _toastNotification.ShowAsync("Invalid Item Details", "Please ensure all item details are correctly filled before adding to the cart.", ToastType.Error);
			return;
		}

		_cart.Add(new()
		{
			LedgerId = _selectedCart.LedgerId,
			LedgerName = _selectedCart.LedgerName,
			Credit = _selectedCart.Credit == 0 ? null : _selectedCart.Credit,
			Debit = _selectedCart.Debit == 0 ? null : _selectedCart.Debit,
			ReferenceId = _selectedCart.ReferenceId,
			ReferenceNo = _selectedCart.ReferenceNo,
			ReferenceType = _selectedCart.ReferenceType,
			InstrumentNo = string.IsNullOrWhiteSpace(_selectedCart.InstrumentNo) ? null : _selectedCart.InstrumentNo.Trim(),
			InstrumentDate = _selectedCart.InstrumentDate,
			Remarks = _selectedCart.Remarks
		});

		_selectedLedger = null;
		_selectedAccountingLedger = null;
		_accountingLedgers = [];
		_selectedCart = new();

		await _sfLedgerAutoComplete.FocusAsync();
		await SaveTransactionFile();
	}

	private async Task EditSelectedCartItem(FinancialAccountingLedgerCartModel cartItem = null)
	{
		if (cartItem is null)
		{
			if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
				return;

			await EditSelectedCartItem(_sfCartGrid.SelectedRecords.First());
			return;
		}

		_selectedLedger = _ledgers.FirstOrDefault(s => s.Id == cartItem.LedgerId);

		if (_selectedLedger is null)
			return;

		_selectedCart = new()
		{
			LedgerId = cartItem.LedgerId,
			LedgerName = cartItem.LedgerName,
			Credit = cartItem.Credit ?? 0,
			Debit = cartItem.Debit ?? 0,
			ReferenceId = cartItem.ReferenceId,
			ReferenceNo = cartItem.ReferenceNo,
			ReferenceType = cartItem.ReferenceType,
			InstrumentNo = cartItem.InstrumentNo,
			InstrumentDate = cartItem.InstrumentDate,
			Remarks = cartItem.Remarks
		};

		await _sfLedgerAutoComplete.FocusAsync();
		await RemoveSelectedCartItem(cartItem);
	}

	private async Task RemoveSelectedCartItem(FinancialAccountingLedgerCartModel cartItem = null)
	{
		if (cartItem is null)
		{
			if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
				return;

			cartItem = _sfCartGrid.SelectedRecords.First();
		}

		_cart.Remove(cartItem);
		await SaveTransactionFile();
	}

	private async Task GetReferences()
	{
		if (_isProcessing || _isLoading)
			return;

		if (_selectedLedger is null || _selectedLedger.Id <= 0)
		{
			await _toastNotification.ShowAsync("Select Ledger", "Please select a ledger first.", ToastType.Warning);
			return;
		}

		try
		{
			_isProcessing = true;

			// Load all accounting ledger transactions for the selected ledger within the financial year
			var allLedgerTransactions = await CommonData.LoadTableDataByDate<FinancialAccountingLedgerOverviewModel>(
				AccountNames.FinancialAccountingLedgerOverview,
				_selectedFinancialYear.StartDate.ToDateTime(TimeOnly.MinValue),
				_accounting.TransactionDateTime);

			// Filter for the selected ledger only
			var ledgerTransactions = allLedgerTransactions.Where(x => x.Id == _selectedLedger.Id).ToList();

			if (ledgerTransactions.Count == 0)
				throw new Exception("No reference transactions found for the selected ledger within the financial year.");

			// Group by ReferenceNo, ReferenceType, and ReferenceId to consolidate transactions
			var ledgerGroups = new Dictionary<(string ReferenceNo, string ReferenceType, int? ReferenceId), FinancialAccountingLedgerOverviewModel>();

			foreach (var item in ledgerTransactions)
			{
				var key = (item.LedgerReferenceNo, item.LedgerReferenceType, item.LedgerReferenceId);

				if (ledgerGroups.TryGetValue(key, out var existingLedger))
				{
					// Aggregate debit and credit amounts	
					existingLedger.Debit = (existingLedger.Debit ?? 0) + (item.Debit ?? 0);
					existingLedger.Credit = (existingLedger.Credit ?? 0) + (item.Credit ?? 0);
				}
				else
					ledgerGroups[key] = item;
			}

			// Filter out balanced entries (where Debit == Credit) and entries without reference information
			_accountingLedgers = [.. ledgerGroups.Values
				.Where(x => (x.Debit ?? 0) != (x.Credit ?? 0) &&
							x.LedgerReferenceId is not null &&
							!string.IsNullOrWhiteSpace(x.LedgerReferenceNo))
				.OrderByDescending(x => x.TransactionDateTime)];

			if (_accountingLedgers.Count == 0)
				throw new Exception("No outstanding references found for the selected ledger. All references are fully balanced.");

			await _toastNotification.ShowAsync("References Loaded", $"Found {_accountingLedgers.Count} outstanding reference(s) for the selected ledger.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Fetching References", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}
	#endregion

	#region Saving
	private async Task UpdateFinancialDetails()
	{
		foreach (var item in _cart.ToList())
		{
			if ((item.Credit ?? 0) == 0)
				item.Credit = null;

			if ((item.Debit ?? 0) == 0)
				item.Debit = null;

			if ((item.Credit ?? 0) > 0 && (item.Debit ?? 0) > 0)
			{
				_cart.Remove(item);
				continue;
			}

			if ((item.Debit ?? 0) < 0 || (item.Credit ?? 0) < 0)
			{
				_cart.Remove(item);
				continue;
			}

			if (item.ReferenceId == 0 || item.ReferenceId is null || string.IsNullOrWhiteSpace(item.ReferenceNo))
			{
				item.ReferenceId = null;
				item.ReferenceNo = null;
				item.ReferenceType = null;
			}
		}

		_accounting.TotalCreditAmount = _cart.Sum(x => x.Credit ?? 0);
		_accounting.TotalDebitAmount = _cart.Sum(x => x.Debit ?? 0);
		_accounting.TotalCreditLedgers = _cart.Count(x => (x.Credit ?? 0) > 0);
		_accounting.TotalDebitLedgers = _cart.Count(x => (x.Debit ?? 0) > 0);

		_accounting.CompanyId = _selectedCompany.Id;
		_accounting.VoucherId = _selectedVoucher.Id;
		_accounting.CreatedBy = _user.Id;

		#region Financial Year
		_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_accounting.TransactionDateTime);
		if (_selectedFinancialYear is not null && !_selectedFinancialYear.Locked)
			_accounting.FinancialYearId = _selectedFinancialYear.Id;
		else
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);
		#endregion

		if (Id is null)
			_accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(_accounting);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_accounting.Status = true;
		_accounting.TransactionDateTime = DateOnly.FromDateTime(_accounting.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
		_accounting.CreatedAt = currentDateTime;
		_accounting.LastModifiedAt = currentDateTime;
		_accounting.CreatedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_accounting.LastModifiedFromPlatform = FormFactor.GetFormFactor() + FormFactor.GetPlatform();
		_accounting.CreatedBy = _user.Id;
		_accounting.LastModifiedBy = _user.Id;
	}

	private async Task SaveTransactionFile()
	{
		if (_isProcessing || _isLoading)
			return;

		try
		{
			_isProcessing = true;

			await UpdateFinancialDetails();

			if (_cart.Count == 0 || _accounting.Id > 0)
			{
				await DeleteLocalFiles();
				return;
			}

			await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingDataFileName, System.Text.Json.JsonSerializer.Serialize(_accounting));
			await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error Saving Transaction Data", ex.Message, ToastType.Error);
		}
		finally
		{
			if (_sfCartGrid is not null)
				await _sfCartGrid?.Refresh();

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

			var ledgers = FinancialAccountingData.ConvertCartToDetails(_cart, _accounting.Id);
			_accounting.Id = await FinancialAccountingData.SaveTransaction(_accounting, ledgers);
			_accounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(AccountNames.FinancialAccounting, _accounting.Id);

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
		if (_accounting.Id <= 0 || (_isProcessing && !force))
			return;

		try
		{
			_isProcessing = true;
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_accounting.TransactionNo, !isExcel, isExcel, CodeType.Accounting);
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
		}
	}

	private async Task ViewReferenceTransaction()
	{
		if (_accounting.ReferenceId is null || _accounting.ReferenceId <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Reference", "No reference transaction found.", ToastType.Error);
			return;
		}

		var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_accounting.ReferenceNo, false, false);
		await AuthenticationService.NavigateToRoute(decodeTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
	}

	private async Task ViewCartReferenceTransaction()
	{
		if (_selectedAccountingLedger is null || _selectedAccountingLedger.LedgerReferenceId is null || _selectedAccountingLedger.LedgerReferenceId <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Reference", "No reference transaction found.", ToastType.Error);
			return;
		}

		var decodeTransactionNo = await DecodeCode.DecodeTransactionNo(_selectedAccountingLedger.LedgerReferenceNo, false, false);
		await AuthenticationService.NavigateToRoute(decodeTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
	}
	#endregion

	#region Utilities
	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<FinancialAccountingLedgerCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart": await EditSelectedCartItem(); break;
			case "DeleteCart": await RemoveSelectedCartItem(); break;
		}
	}

	private async Task DeleteLocalFiles()
	{
		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingDataFileName);
		await DataStorageService.LocalRemove(StorageFileNames.FinancialAccountingCartDataFileName);
	}

	private async Task ResetPage()
	{
		await DeleteLocalFiles();
		PageRefresh.Request();
	}
	#endregion
}

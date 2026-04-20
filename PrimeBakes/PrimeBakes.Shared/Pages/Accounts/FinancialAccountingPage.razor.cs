using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Data.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Operations;
using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Exporting.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;

using Syncfusion.Blazor.DropDowns;
using Syncfusion.Blazor.Grids;

namespace PrimeBakes.Shared.Pages.Accounts;

public partial class FinancialAccountingPage : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;

	[Parameter] public int? Id { get; set; }

	private UserModel _user;

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private CompanyModel _selectedCompany = new();
	private VoucherModel _selectedVoucher = new();
	private FinancialYearModel _selectedFinancialYear = new();
	private LedgerModel? _selectedLedger = null;
	private FinancialAccountingLedgerOverviewModel? _selectedAccountingLedger = null;
	private FinancialAccountingItemCartModel _selectedCart = new();
	private FinancialAccountingModel _accounting = new();

	private List<CompanyModel> _companies = [];
	private List<VoucherModel> _vouchers = [];
	private List<LedgerModel> _ledgers = [];
	private List<FinancialAccountingLedgerOverviewModel> _accountingLedgers = [];
	private List<FinancialAccountingItemCartModel> _cart = [];
	private readonly List<ContextMenuItemModel> _cartGridContextMenuItems =
	[
		new() { Text = "Edit (Insert)", Id = "EditCart", IconCss = "e-icons e-edit", Target = ".e-content" },
		new() { Text = "Delete (Del)", Id = "DeleteCart", IconCss = "e-icons e-trash", Target = ".e-content" }
	];

	private SfAutoComplete<LedgerModel?, LedgerModel> _sfLedgerAutoComplete;
	private SfGrid<FinancialAccountingItemCartModel> _sfCartGrid;

	private ToastNotification _toastNotification;

	#region Load Data
	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Accounts], true);
		await InitializePage();
	}

	private async Task InitializePage()
	{
		LoadHotKeys();
		await LoadCompanies();
		await LoadVouchers();

		if (!await ResolveTransaction())
			return;

		await LoadSelections();
		await LoadLedgers();
		await LoadCart();
		await SaveTransactionFile();

		_isLoading = false;
		StateHasChanged();
	}

	private async Task<bool> ResolveTransaction()
	{
		try
		{
			if (await LoadExistingTransaction())
				return true;

			if (await TryRestoreFromLocalStorage())
				return true;

			await CreateNewTransaction();
			return true;
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Transaction Data", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
			await CreateNewTransaction();
			return true;
		}
	}

	private async Task<bool> LoadExistingTransaction()
	{
		if (!Id.HasValue)
			return false;

		_accounting = await CommonData.LoadTableDataById<FinancialAccountingModel>(TableNames.FinancialAccounting, Id.Value);
		if (_accounting is null || _accounting.Id == 0)
		{
			await _toastNotification.ShowAsync("Transaction Not Found", "The requested transaction could not be found.", ToastType.Error);
			NavigationManager.NavigateTo(PageRouteNames.FinancialAccounting, true);
			return false;
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
			_selectedFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, _accounting.FinancialYearId);

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
				var existingCart = await CommonData.LoadTableDataByMasterId<FinancialAccountingDetailModel>(TableNames.FinancialAccountingDetail, _accounting.Id);

				foreach (var item in existingCart)
				{
					if (_ledgers.FirstOrDefault(s => s.Id == item.LedgerId) is null)
					{
						var ledger = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, item.LedgerId);
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
				_cart = System.Text.Json.JsonSerializer.Deserialize<List<FinancialAccountingItemCartModel>>(await DataStorageService.LocalGetAsync(StorageFileNames.FinancialAccountingCartDataFileName));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Existing Cart", ex.Message, ToastType.Error);
			await DeleteLocalFiles();
		}
	}

	private async Task LoadCompanies()
	{
		try
		{
			_companies = await CommonData.LoadTableDataByStatus<CompanyModel>(TableNames.Company);
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
			_vouchers = await CommonData.LoadTableDataByStatus<VoucherModel>(TableNames.Voucher);
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
			_ledgers = await CommonData.LoadTableDataByStatus<LedgerModel>(TableNames.Ledger);
			_ledgers = [.. _ledgers.OrderBy(s => s.Name)];
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Loading Ledgers", ex.Message, ToastType.Error);
		}
	}
	#endregion

	#region Change Events
	private async Task OnCompanyChanged(ChangeEventArgs<CompanyModel, CompanyModel> args)
	{
		if (args.Value is null || args.Value.Id == 0)
			return;

		_selectedCompany = args.Value;
		_accounting.CompanyId = _selectedCompany.Id;

		await SaveTransactionFile();
	}

	private async Task OnPartyChanged(ChangeEventArgs<VoucherModel, VoucherModel> args)
	{
		if (args.Value is null || args.Value.Id == 0)
			return;

		_selectedVoucher = args.Value;
		_accounting.VoucherId = _selectedVoucher.Id;

		await SaveTransactionFile();
	}

	private async Task OnTransactionDateChanged(Syncfusion.Blazor.Calendars.ChangedEventArgs<DateTime> args)
	{
		_accounting.TransactionDateTime = args.Value;
		await SaveTransactionFile();
	}
	#endregion

	#region Cart
	private async Task OnItemChanged(ChangeEventArgs<LedgerModel?, LedgerModel?> args)
	{
		if (args.Value is null || args.Value.Id == 0)
			return;

		_selectedLedger = args.Value;

		if (_selectedLedger is null)
			_selectedCart = new()
			{
				LedgerId = 0,
				LedgerName = string.Empty,
				Credit = null,
				Debit = null,
				ReferenceId = null,
				ReferenceNo = null,
				ReferenceType = null,
				Remarks = string.Empty
			};

		else
		{
			_selectedCart.LedgerId = _selectedLedger.Id;
			_selectedCart.LedgerName = _selectedLedger.Name;
			_selectedCart.Credit = null;
			_selectedCart.Debit = null;
		}
	}

	private void OnReferenceChanged(ChangeEventArgs<FinancialAccountingLedgerOverviewModel, FinancialAccountingLedgerOverviewModel> args)
	{
		if (args.Value is null)
		{
			_selectedAccountingLedger = null;
			_selectedCart.ReferenceNo = null;
			_selectedCart.ReferenceId = null;
			_selectedCart.ReferenceType = null;
			return;
		}

		_selectedAccountingLedger = args.Value;
		_selectedCart.ReferenceNo = args.Value.ReferenceNo;
		_selectedCart.ReferenceId = args.Value.ReferenceId;
		_selectedCart.ReferenceType = args.Value.ReferenceType;

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
			Remarks = _selectedCart.Remarks
		});

		_selectedLedger = null;
		_selectedAccountingLedger = null;
		_accountingLedgers = [];
		_selectedCart = new();

		await _sfLedgerAutoComplete.FocusAsync();
		await SaveTransactionFile();
	}

	private async Task EditSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await EditCartItem(selectedCartItem);
	}

	private async Task EditCartItem(FinancialAccountingItemCartModel cartItem)
	{
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
			Remarks = cartItem.Remarks
		};

		await _sfLedgerAutoComplete.FocusAsync();
		await RemoveItemFromCart(cartItem);
	}

	private async Task RemoveSelectedCartItem()
	{
		if (_sfCartGrid is null || _sfCartGrid.SelectedRecords is null || _sfCartGrid.SelectedRecords.Count == 0)
			return;

		var selectedCartItem = _sfCartGrid.SelectedRecords.First();
		await RemoveItemFromCart(selectedCartItem);
	}

	private async Task RemoveItemFromCart(FinancialAccountingItemCartModel cartItem)
	{
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
				ViewNames.FinancialAccountingLedgerOverview,
				_selectedFinancialYear.StartDate.ToDateTime(TimeOnly.MinValue),
				_accounting.TransactionDateTime);

			// Filter for the selected ledger only
			var ledgerTransactions = allLedgerTransactions.Where(x => x.Id == _selectedLedger.Id).ToList();

			if (ledgerTransactions.Count == 0)
			{
				await _toastNotification.ShowAsync("No References Found", "No reference transactions found for the selected ledger within the financial year.", ToastType.Info);
				return;
			}

			// Group by ReferenceNo, ReferenceType, and ReferenceId to consolidate transactions
			var ledgerGroups = new Dictionary<(string ReferenceNo, string ReferenceType, int? ReferenceId), FinancialAccountingLedgerOverviewModel>();

			foreach (var item in ledgerTransactions)
			{
				var key = (item.ReferenceNo, item.ReferenceType, item.ReferenceId);

				if (ledgerGroups.TryGetValue(key, out var existingLedger))
				{
					// Aggregate debit and credit amounts	
					existingLedger.Debit = (existingLedger.Debit ?? 0) + (item.Debit ?? 0);
					existingLedger.Credit = (existingLedger.Credit ?? 0) + (item.Credit ?? 0);
				}
				else
				{
					ledgerGroups[key] = item;
				}
			}

			// Filter out balanced entries (where Debit == Credit) and entries without reference information
			_accountingLedgers = [.. ledgerGroups.Values
				.Where(x => (x.Debit ?? 0) != (x.Credit ?? 0) &&
							x.ReferenceId is not null &&
							!string.IsNullOrEmpty(x.ReferenceNo))
				.OrderByDescending(x => x.ReferenceDateTime)];

			if (_accountingLedgers.Count == 0)
			{
				await _toastNotification.ShowAsync("No Outstanding References", "All references for this ledger are fully balanced.", ToastType.Info);
				return;
			}

			await _toastNotification.ShowAsync("References Loaded", $"Found {_accountingLedgers.Count} outstanding reference(s) for the selected ledger.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Fetching References", ex.Message, ToastType.Error);
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

			item.Remarks = item.Remarks?.Trim();
			if (string.IsNullOrWhiteSpace(item.Remarks))
				item.Remarks = null;
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
		{
			await _toastNotification.ShowAsync("Invalid Transaction Date", "The selected transaction date does not fall within an active financial year.", ToastType.Error);
			_accounting.TransactionDateTime = await CommonData.LoadCurrentDateTime();
			_selectedFinancialYear = await FinancialYearData.LoadFinancialYearByDateTime(_accounting.TransactionDateTime);
			_accounting.FinancialYearId = _selectedFinancialYear.Id;
		}
		#endregion

		if (Id is null)
			_accounting.TransactionNo = await GenerateCodes.GenerateAccountingTransactionNo(_accounting);

		_accounting.Remarks = _accounting.Remarks?.Trim();
		if (string.IsNullOrWhiteSpace(_accounting.Remarks))
			_accounting.Remarks = null;

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		_accounting.Status = true;
		_accounting.TransactionDateTime = DateOnly.FromDateTime(_accounting.TransactionDateTime).ToDateTime(new TimeOnly(currentDateTime.Hour, currentDateTime.Minute, currentDateTime.Second));
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

			await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingDataFileName, System.Text.Json.JsonSerializer.Serialize(_accounting));
			await DataStorageService.LocalSaveAsync(StorageFileNames.FinancialAccountingCartDataFileName, System.Text.Json.JsonSerializer.Serialize(_cart));
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Saving Transaction Data", ex.Message, ToastType.Error);
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

			await _toastNotification.ShowAsync("Processing Transaction", "Please wait while the transaction is being saved...", ToastType.Info);

			_accounting.Id = await FinancialAccountingData.SaveTransaction(_accounting, _cart);

			if (savePDF)
			{
				var (pdfStream, pdfFileName) = await FinancialAccountingInvoiceExport.ExportInvoice(_accounting.Id, InvoiceExportType.PDF);
				await SaveAndViewService.SaveAndView(pdfFileName, pdfStream);
			}

			if (saveExcel)
			{
				var (excelStream, excelFileName) = await FinancialAccountingInvoiceExport.ExportInvoice(_accounting.Id, InvoiceExportType.Excel);
				await SaveAndViewService.SaveAndView(excelFileName, excelStream);
			}

			await ResetPage();
			await _toastNotification.ShowAsync("Save Transaction", "Transaction saved successfully.", ToastType.Success);
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

	#region Exporting
	private async Task ExportPdfInvoice()
	{
		if (!Id.HasValue || Id.Value <= 0)
		{
			await _toastNotification.ShowAsync("Nothing to Export", "There is nothing to export.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await GenerateCodes.DecodeTransactionNo(_accounting.TransactionNo);
			await SaveAndViewService.SaveAndView(decodeTransactionNo.PDFStream.fileName, decodeTransactionNo.PDFStream.stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Exporting", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task ExportExcelInvoice()
	{
		if (!Id.HasValue || Id.Value <= 0)
		{
			await _toastNotification.ShowAsync("Nothing to Export", "There is nothing to export.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await GenerateCodes.DecodeTransactionNo(_accounting.TransactionNo);
			await SaveAndViewService.SaveAndView(decodeTransactionNo.ExcelStream.fileName, decodeTransactionNo.ExcelStream.stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task ExportReferenceInvoice()
	{
		if (_accounting.ReferenceId is null or <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Reference", "No reference transaction found.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await GenerateCodes.DecodeTransactionNo(_accounting.ReferenceNo);
			await SaveAndViewService.SaveAndView(decodeTransactionNo.PDFStream.fileName, decodeTransactionNo.PDFStream.stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task ExportCartReferenceInvoice()
	{
		if (_selectedAccountingLedger is null || _selectedAccountingLedger.ReferenceId is null || _selectedAccountingLedger.ReferenceId <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Reference", "No reference transaction found.", ToastType.Error);
			return;
		}

		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			await _toastNotification.ShowAsync("Processing", "Generating the Export...", ToastType.Info);

			var decodeTransactionNo = await GenerateCodes.DecodeTransactionNo(_selectedAccountingLedger.ReferenceNo);
			await SaveAndViewService.SaveAndView(decodeTransactionNo.PDFStream.fileName, decodeTransactionNo.PDFStream.stream);

			await _toastNotification.ShowAsync("Exported", "The export has been downloaded successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("An Error Occurred While Downloading Invoice", ex.Message, ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
		}
	}

	private async Task ViewReferenceInvoice()
	{
		if (_accounting.ReferenceId is null || _accounting.ReferenceId <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Reference", "No reference transaction found.", ToastType.Error);
			return;
		}

		var decodeTransactionNo = await GenerateCodes.DecodeTransactionNo(_accounting.ReferenceNo);
		await AuthenticationService.NavigateToRoute(decodeTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
	}

	private async Task ViewCartReferenceInvoice()
	{
		if (_selectedAccountingLedger is null || _selectedAccountingLedger.ReferenceId is null || _selectedAccountingLedger.ReferenceId <= 0)
		{
			await _toastNotification.ShowAsync("Invalid Reference", "No reference transaction found.", ToastType.Error);
			return;
		}

		var decodeTransactionNo = await GenerateCodes.DecodeTransactionNo(_selectedAccountingLedger.ReferenceNo);
		await AuthenticationService.NavigateToRoute(decodeTransactionNo.PageRouteName, FormFactor, JSRuntime, NavigationManager);
	}
	#endregion

	#region Utilities
	private void LoadHotKeys()
	{
		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.Enter, AddItemToCart, "Add item to cart", Exclude.None)
			.Add(ModCode.Ctrl, Code.F, () => _sfLedgerAutoComplete.FocusAsync(), "Focus on ledger input", Exclude.None)
			.Add(ModCode.Ctrl, Code.S, () => SaveTransaction(), "Save the transaction", Exclude.None)
			.Add(ModCode.Ctrl, Code.P, () => SaveTransaction(savePDF: true), "Save & PDF", Exclude.None)
			.Add(ModCode.Ctrl, Code.E, () => SaveTransaction(saveExcel: true), "Save & Excel", Exclude.None)
			.Add(ModCode.Alt, Code.P, ExportPdfInvoice, "Export PDF", Exclude.None)
			.Add(ModCode.Alt, Code.E, ExportExcelInvoice, "Export Excel", Exclude.None)
			.Add(ModCode.Ctrl, Code.H, () => AuthenticationService.NavigateToRoute(PageRouteNames.FinancialAccountingReport, FormFactor, JSRuntime, NavigationManager), "Open transaction history", Exclude.None)
			.Add(ModCode.Ctrl, Code.N, ResetPage, "Reset the page", Exclude.None)
			.Add(ModCode.Ctrl, Code.B, NavigateBack, "Back", Exclude.None)
			.Add(Code.Delete, RemoveSelectedCartItem, "Delete selected cart item", Exclude.None)
			.Add(Code.Insert, EditSelectedCartItem, "Edit selected cart item", Exclude.None);
	}

	private async Task OnMenuSelected(Syncfusion.Blazor.Navigations.MenuEventArgs<Syncfusion.Blazor.Navigations.MenuItem> args)
	{
		switch (args.Item.Id)
		{
			case "NewTransaction":
				await ResetPage();
				break;
			case "SaveTransaction":
				await SaveTransaction();
				break;
			case "SavePdfInvoice":
				await SaveTransaction(savePDF: true);
				break;
			case "SaveExcelInvoice":
				await SaveTransaction(saveExcel: true);
				break;
			case "ExportPdfInvoice":
				await ExportPdfInvoice();
				break;
			case "ExportExcelInvoice":
				await ExportExcelInvoice();
				break;
			case "TransactionHistory":
				await AuthenticationService.NavigateToRoute(PageRouteNames.FinancialAccountingReport, FormFactor, JSRuntime, NavigationManager);
				break;
			case "ItemReport":
				await AuthenticationService.NavigateToRoute(PageRouteNames.AccountingLedgerReport, FormFactor, JSRuntime, NavigationManager);
				break;
			case "TrialBalance":
				await AuthenticationService.NavigateToRoute(PageRouteNames.TrialBalanceReport, FormFactor, JSRuntime, NavigationManager);
				break;
			case "ProfitLoss":
				await AuthenticationService.NavigateToRoute(PageRouteNames.ProfitAndLossReport, FormFactor, JSRuntime, NavigationManager);
				break;
			case "BalanceSheet":
				await AuthenticationService.NavigateToRoute(PageRouteNames.BalanceSheetReport, FormFactor, JSRuntime, NavigationManager);
				break;
		}
	}

	private async Task OnCartGridContextMenuItemClicked(ContextMenuClickEventArgs<FinancialAccountingItemCartModel> args)
	{
		switch (args.Item.Id)
		{
			case "EditCart":
				await EditSelectedCartItem();
				break;
			case "DeleteCart":
				await RemoveSelectedCartItem();
				break;
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
		NavigationManager.NavigateTo(PageRouteNames.FinancialAccounting, true);
	}

	private void NavigateBack() =>
		NavigationManager.NavigateTo(PageRouteNames.AccountsDashboard);

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();

		GC.SuppressFinalize(this);
	}
	#endregion
}

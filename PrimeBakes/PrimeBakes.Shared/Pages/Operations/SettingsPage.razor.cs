using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.Settings;
using PrimeBakesLibrary.Operations.User;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class SettingsPage
{
	#region Fields

	private bool _isLoading = true;
	private bool _isProcessing = false;

	private ToastNotification _toastNotification;
	private ConfirmationDialog _confirmationDialog;

	private string _confirmTitle = string.Empty;
	private string _confirmMessage = string.Empty;
	private Func<Task> _confirmAction;

	// Primary Configuration
	private string _primaryCompanyLinkingId = string.Empty;
	private CompanyModel _selectedCompany;
	private List<CompanyModel> _companies = [];

	// Code Prefixes
	private string _rawMaterialCodePrefix = string.Empty;
	private string _finishedProductCodePrefix = string.Empty;
	private string _ledgerCodePrefix = string.Empty;

	// Transaction Prefixes
	private string _accountingTransactionPrefix = string.Empty;
	private string _purchaseTransactionPrefix = string.Empty;
	private string _purchaseReturnTransactionPrefix = string.Empty;
	private string _kitchenIssueTransactionPrefix = string.Empty;
	private string _kitchenProductionTransactionPrefix = string.Empty;
	private string _rawMaterialStockAdjustmentTransactionPrefix = string.Empty;
	private string _productStockAdjustmentTransactionPrefix = string.Empty;
	private string _saleTransactionPrefix = string.Empty;
	private string _saleReturnTransactionPrefix = string.Empty;
	private string _stockTransferTransactionPrefix = string.Empty;
	private string _orderTransactionPrefix = string.Empty;
	private string _billTransactionPrefix = string.Empty;

	// Vouchers
	private string _purchaseVoucherId = string.Empty;
	private VoucherModel _selectedPurchaseVoucher;
	private string _purchaseReturnVoucherId = string.Empty;
	private VoucherModel _selectedPurchaseReturnVoucher;
	private string _saleVoucherId = string.Empty;
	private VoucherModel _selectedSaleVoucher;
	private string _saleReturnVoucherId = string.Empty;
	private VoucherModel _selectedSaleReturnVoucher;
	private string _stockTransferVoucherId = string.Empty;
	private VoucherModel _selectedStockTransferVoucher;
	private string _billVoucherId = string.Empty;
	private VoucherModel _selectedBillVoucher;
	private string _billDayCloseVoucherId = string.Empty;
	private VoucherModel _selectedBillDayCloseVoucher;
	private string _saleDayCloseVoucherId = string.Empty;
	private VoucherModel _selectedSaleDayCloseVoucher;
	private string _defaultSelectedVoucherId = string.Empty;
	private VoucherModel _selectedDefaultVoucher;
	private List<VoucherModel> _vouchers = [];

	// Ledgers
	private string _purchaseLedgerId = string.Empty;
	private LedgerModel _selectedPurchaseLedger;
	private string _saleLedgerId = string.Empty;
	private LedgerModel _selectedSaleLedger;
	private string _stockTransferLedgerId = string.Empty;
	private LedgerModel _selectedStockTransferLedger;
	private string _billLedgerId = string.Empty;
	private LedgerModel _selectedBillLedger;
	private string _cashLedgerId = string.Empty;
	private LedgerModel _selectedCashLedger;
	private string _cashSalesLedgerId = string.Empty;
	private LedgerModel _selectedCashSalesLedger;
	private string _gstLedgerId = string.Empty;
	private LedgerModel _selectedGSTLedger;
	private List<LedgerModel> _ledgers = [];

	// Bank Reconciliation
	private string _bankAccountTypeId = string.Empty;
	private AccountTypeModel _selectedBankAccountType;
	private List<AccountTypeModel> _accountTypes = [];

	// Purchase Behavior
	private bool _updateItemMasterRateOnPurchase = false;
	private bool _updateItemMasterUOMOnPurchase = false;

	// Report Settings
	private int _autoRefreshReportTimer = 5;
	private int _reportWarningDays = 30;

	// Notification Settings
	private string _notificationEmail = string.Empty;

	#endregion

	#region Load Data

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		try
		{
			await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Admin], true);
			await LoadData();
			_isLoading = false;
			StateHasChanged();
		}
		catch { NavigationManager.NavigateTo(OperationRouteNames.Dashboard); }
	}

	private async Task LoadData()
	{
		try
		{
			await LoadAllSettings();
			await LoadCompanies();
			await LoadVouchers();
			await LoadLedgers();
			await LoadAccountTypes();
			MapSelections();
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to load settings: {ex.Message}", ToastType.Error);
		}
	}

	private async Task LoadAllSettings()
	{
		var map = (await CommonData.LoadTableData<SettingsModel>(OperationNames.Settings) ?? [])
			.ToDictionary(s => s.Key, s => s.Value);

		string Str(string key) => map.TryGetValue(key, out var v) ? v : null;
		int Int(string key, int fallback) => int.TryParse(Str(key), out var v) ? v : fallback;
		bool Bool(string key, bool fallback) => bool.TryParse(Str(key), out var v) ? v : fallback;

		// Primary Configuration
		_primaryCompanyLinkingId = Str(SettingsKeys.PrimaryCompanyLinkingId) ?? string.Empty;

		// Code Prefixes
		_rawMaterialCodePrefix = Str(SettingsKeys.RawMaterialCodePrefix) ?? string.Empty;
		_finishedProductCodePrefix = Str(SettingsKeys.FinishedProductCodePrefix) ?? string.Empty;
		_ledgerCodePrefix = Str(SettingsKeys.LedgerCodePrefix) ?? string.Empty;

		// Transaction Prefixes
		_accountingTransactionPrefix = Str(SettingsKeys.AccountingTransactionPrefix) ?? string.Empty;
		_purchaseTransactionPrefix = Str(SettingsKeys.PurchaseTransactionPrefix) ?? string.Empty;
		_purchaseReturnTransactionPrefix = Str(SettingsKeys.PurchaseReturnTransactionPrefix) ?? string.Empty;
		_kitchenIssueTransactionPrefix = Str(SettingsKeys.KitchenIssueTransactionPrefix) ?? string.Empty;
		_kitchenProductionTransactionPrefix = Str(SettingsKeys.KitchenProductionTransactionPrefix) ?? string.Empty;
		_rawMaterialStockAdjustmentTransactionPrefix = Str(SettingsKeys.RawMaterialStockAdjustmentTransactionPrefix) ?? string.Empty;
		_productStockAdjustmentTransactionPrefix = Str(SettingsKeys.ProductStockAdjustmentTransactionPrefix) ?? string.Empty;
		_saleTransactionPrefix = Str(SettingsKeys.SaleTransactionPrefix) ?? string.Empty;
		_saleReturnTransactionPrefix = Str(SettingsKeys.SaleReturnTransactionPrefix) ?? string.Empty;
		_stockTransferTransactionPrefix = Str(SettingsKeys.StockTransferTransactionPrefix) ?? string.Empty;
		_orderTransactionPrefix = Str(SettingsKeys.OrderTransactionPrefix) ?? string.Empty;
		_billTransactionPrefix = Str(SettingsKeys.BillTransactionPrefix) ?? string.Empty;

		// Vouchers
		_purchaseVoucherId = Str(SettingsKeys.PurchaseVoucherId) ?? string.Empty;
		_purchaseReturnVoucherId = Str(SettingsKeys.PurchaseReturnVoucherId) ?? string.Empty;
		_saleVoucherId = Str(SettingsKeys.SaleVoucherId) ?? string.Empty;
		_saleReturnVoucherId = Str(SettingsKeys.SaleReturnVoucherId) ?? string.Empty;
		_stockTransferVoucherId = Str(SettingsKeys.StockTransferVoucherId) ?? string.Empty;
		_billVoucherId = Str(SettingsKeys.BillVoucherId) ?? string.Empty;
		_billDayCloseVoucherId = Str(SettingsKeys.BillDayCloseVoucherId) ?? string.Empty;
		_saleDayCloseVoucherId = Str(SettingsKeys.SaleDayCloseVoucherId) ?? string.Empty;
		_defaultSelectedVoucherId = Str(SettingsKeys.DefaultSelectedVoucherId) ?? string.Empty;

		// Ledgers
		_purchaseLedgerId = Str(SettingsKeys.PurchaseLedgerId) ?? string.Empty;
		_saleLedgerId = Str(SettingsKeys.SaleLedgerId) ?? string.Empty;
		_stockTransferLedgerId = Str(SettingsKeys.StockTransferLedgerId) ?? string.Empty;
		_billLedgerId = Str(SettingsKeys.BillLedgerId) ?? string.Empty;
		_cashLedgerId = Str(SettingsKeys.CashLedgerId) ?? string.Empty;
		_cashSalesLedgerId = Str(SettingsKeys.CashSalesLedgerId) ?? string.Empty;
		_gstLedgerId = Str(SettingsKeys.GSTLedgerId) ?? string.Empty;

		// Bank Reconciliation
		_bankAccountTypeId = Str(SettingsKeys.BankAccountTypeId) ?? string.Empty;

		// Purchase Behavior
		_updateItemMasterRateOnPurchase = Bool(SettingsKeys.UpdateItemMasterRateOnPurchase, false);
		_updateItemMasterUOMOnPurchase = Bool(SettingsKeys.UpdateItemMasterUOMOnPurchase, false);

		// Report Settings
		_autoRefreshReportTimer = Int(SettingsKeys.AutoRefreshReportTimer, 5);
		_reportWarningDays = Int(SettingsKeys.ReportWarningDays, 30);

		// Notification Settings
		_notificationEmail = Str(SettingsKeys.NotificationEmail) ?? string.Empty;
	}

	private async Task LoadCompanies()
	{
		var result = await CommonData.LoadTableData<CompanyModel>(AccountNames.Company);
		_companies = result ?? [];
	}

	private async Task LoadVouchers()
	{
		var result = await CommonData.LoadTableData<VoucherModel>(AccountNames.Voucher);
		_vouchers = result ?? [];
	}

	private async Task LoadLedgers()
	{
		var result = await CommonData.LoadTableData<LedgerModel>(AccountNames.Ledger);
		_ledgers = result ?? [];
	}

	private async Task LoadAccountTypes()
	{
		var result = await CommonData.LoadTableData<AccountTypeModel>(AccountNames.AccountType);
		_accountTypes = result ?? [];
	}

	private void MapSelections()
	{
		if (!string.IsNullOrWhiteSpace(_primaryCompanyLinkingId) && int.TryParse(_primaryCompanyLinkingId, out var companyId))
			_selectedCompany = _companies.FirstOrDefault(c => c.Id == companyId);

		// Vouchers
		if (!string.IsNullOrWhiteSpace(_purchaseVoucherId) && int.TryParse(_purchaseVoucherId, out var purchaseVoucherId))
			_selectedPurchaseVoucher = _vouchers.FirstOrDefault(v => v.Id == purchaseVoucherId);

		if (!string.IsNullOrWhiteSpace(_purchaseReturnVoucherId) && int.TryParse(_purchaseReturnVoucherId, out var purchaseReturnVoucherId))
			_selectedPurchaseReturnVoucher = _vouchers.FirstOrDefault(v => v.Id == purchaseReturnVoucherId);

		if (!string.IsNullOrWhiteSpace(_saleVoucherId) && int.TryParse(_saleVoucherId, out var saleVoucherId))
			_selectedSaleVoucher = _vouchers.FirstOrDefault(v => v.Id == saleVoucherId);

		if (!string.IsNullOrWhiteSpace(_saleReturnVoucherId) && int.TryParse(_saleReturnVoucherId, out var saleReturnVoucherId))
			_selectedSaleReturnVoucher = _vouchers.FirstOrDefault(v => v.Id == saleReturnVoucherId);

		if (!string.IsNullOrWhiteSpace(_stockTransferVoucherId) && int.TryParse(_stockTransferVoucherId, out var stockTransferVoucherId))
			_selectedStockTransferVoucher = _vouchers.FirstOrDefault(v => v.Id == stockTransferVoucherId);

		if (!string.IsNullOrWhiteSpace(_billVoucherId) && int.TryParse(_billVoucherId, out var billVoucherId))
			_selectedBillVoucher = _vouchers.FirstOrDefault(v => v.Id == billVoucherId);

		if (!string.IsNullOrWhiteSpace(_billDayCloseVoucherId) && int.TryParse(_billDayCloseVoucherId, out var billDayCloseVoucherId))
			_selectedBillDayCloseVoucher = _vouchers.FirstOrDefault(v => v.Id == billDayCloseVoucherId);

		if (!string.IsNullOrWhiteSpace(_saleDayCloseVoucherId) && int.TryParse(_saleDayCloseVoucherId, out var saleDayCloseVoucherId))
			_selectedSaleDayCloseVoucher = _vouchers.FirstOrDefault(v => v.Id == saleDayCloseVoucherId);

		if (!string.IsNullOrWhiteSpace(_defaultSelectedVoucherId) && int.TryParse(_defaultSelectedVoucherId, out var defaultVoucherId))
			_selectedDefaultVoucher = _vouchers.FirstOrDefault(v => v.Id == defaultVoucherId);

		// Ledgers
		if (!string.IsNullOrWhiteSpace(_purchaseLedgerId) && int.TryParse(_purchaseLedgerId, out var purchaseLedgerId))
			_selectedPurchaseLedger = _ledgers.FirstOrDefault(l => l.Id == purchaseLedgerId);

		if (!string.IsNullOrWhiteSpace(_saleLedgerId) && int.TryParse(_saleLedgerId, out var saleLedgerId))
			_selectedSaleLedger = _ledgers.FirstOrDefault(l => l.Id == saleLedgerId);

		if (!string.IsNullOrWhiteSpace(_stockTransferLedgerId) && int.TryParse(_stockTransferLedgerId, out var stockTransferLedgerId))
			_selectedStockTransferLedger = _ledgers.FirstOrDefault(l => l.Id == stockTransferLedgerId);

		if (!string.IsNullOrWhiteSpace(_billLedgerId) && int.TryParse(_billLedgerId, out var billLedgerId))
			_selectedBillLedger = _ledgers.FirstOrDefault(l => l.Id == billLedgerId);

		if (!string.IsNullOrWhiteSpace(_cashLedgerId) && int.TryParse(_cashLedgerId, out var cashLedgerId))
			_selectedCashLedger = _ledgers.FirstOrDefault(l => l.Id == cashLedgerId);

		if (!string.IsNullOrWhiteSpace(_cashSalesLedgerId) && int.TryParse(_cashSalesLedgerId, out var cashSalesLedgerId))
			_selectedCashSalesLedger = _ledgers.FirstOrDefault(l => l.Id == cashSalesLedgerId);

		if (!string.IsNullOrWhiteSpace(_gstLedgerId) && int.TryParse(_gstLedgerId, out var gstLedgerId))
			_selectedGSTLedger = _ledgers.FirstOrDefault(l => l.Id == gstLedgerId);

		// Bank Reconciliation
		if (!string.IsNullOrWhiteSpace(_bankAccountTypeId) && int.TryParse(_bankAccountTypeId, out var bankAccountTypeId))
			_selectedBankAccountType = _accountTypes.FirstOrDefault(a => a.Id == bankAccountTypeId);
	}

	#endregion

	#region Change Handlers

	private void OnCompanyChange(CompanyModel value)
	{
		_selectedCompany = value;
		_primaryCompanyLinkingId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnPurchaseVoucherChange(VoucherModel value)
	{
		_selectedPurchaseVoucher = value;
		_purchaseVoucherId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnPurchaseReturnVoucherChange(VoucherModel value)
	{
		_selectedPurchaseReturnVoucher = value;
		_purchaseReturnVoucherId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnSaleVoucherChange(VoucherModel value)
	{
		_selectedSaleVoucher = value;
		_saleVoucherId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnSaleReturnVoucherChange(VoucherModel value)
	{
		_selectedSaleReturnVoucher = value;
		_saleReturnVoucherId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnStockTransferVoucherChange(VoucherModel value)
	{
		_selectedStockTransferVoucher = value;
		_stockTransferVoucherId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnBillVoucherChange(VoucherModel value)
	{
		_selectedBillVoucher = value;
		_billVoucherId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnBillDayCloseVoucherChange(VoucherModel value)
	{
		_selectedBillDayCloseVoucher = value;
		_billDayCloseVoucherId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnSaleDayCloseVoucherChange(VoucherModel value)
	{
		_selectedSaleDayCloseVoucher = value;
		_saleDayCloseVoucherId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnDefaultVoucherChange(VoucherModel value)
	{
		_selectedDefaultVoucher = value;
		_defaultSelectedVoucherId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnPurchaseLedgerChange(LedgerModel value)
	{
		_selectedPurchaseLedger = value;
		_purchaseLedgerId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnSaleLedgerChange(LedgerModel value)
	{
		_selectedSaleLedger = value;
		_saleLedgerId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnStockTransferLedgerChange(LedgerModel value)
	{
		_selectedStockTransferLedger = value;
		_stockTransferLedgerId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnBillLedgerChange(LedgerModel value)
	{
		_selectedBillLedger = value;
		_billLedgerId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnCashLedgerChange(LedgerModel value)
	{
		_selectedCashLedger = value;
		_cashLedgerId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnCashSalesLedgerChange(LedgerModel value)
	{
		_selectedCashSalesLedger = value;
		_cashSalesLedgerId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnGSTLedgerChange(LedgerModel value)
	{
		_selectedGSTLedger = value;
		_gstLedgerId = value?.Id.ToString() ?? string.Empty;
	}

	private void OnBankAccountTypeChange(AccountTypeModel value)
	{
		_selectedBankAccountType = value;
		_bankAccountTypeId = value?.Id.ToString() ?? string.Empty;
	}

	#endregion

	#region Save Settings

	private async Task SaveSettings()
	{
		if (_isProcessing)
			return;

		try
		{
			_isProcessing = true;
			StateHasChanged();

			if (string.IsNullOrWhiteSpace(_primaryCompanyLinkingId))
			{
				await _toastNotification.ShowAsync("Validation", "Primary Company is required.", ToastType.Warning);
				return;
			}

			await _toastNotification.ShowAsync("Saving", "Processing settings...", ToastType.Info);

			var settings = await CommonData.LoadTableData<SettingsModel>(OperationNames.Settings);
			string Desc(string key) => settings.FirstOrDefault(s => s.Key == key)?.Description ?? string.Empty;

			// Primary Configuration
			await UpdateSetting(SettingsKeys.PrimaryCompanyLinkingId, _primaryCompanyLinkingId, Desc(SettingsKeys.PrimaryCompanyLinkingId));

			// Code Prefixes
			await UpdateSetting(SettingsKeys.RawMaterialCodePrefix, _rawMaterialCodePrefix, Desc(SettingsKeys.RawMaterialCodePrefix));
			await UpdateSetting(SettingsKeys.FinishedProductCodePrefix, _finishedProductCodePrefix, Desc(SettingsKeys.FinishedProductCodePrefix));
			await UpdateSetting(SettingsKeys.LedgerCodePrefix, _ledgerCodePrefix, Desc(SettingsKeys.LedgerCodePrefix));

			// Transaction Prefixes
			await UpdateSetting(SettingsKeys.AccountingTransactionPrefix, _accountingTransactionPrefix, Desc(SettingsKeys.AccountingTransactionPrefix));
			await UpdateSetting(SettingsKeys.PurchaseTransactionPrefix, _purchaseTransactionPrefix, Desc(SettingsKeys.PurchaseTransactionPrefix));
			await UpdateSetting(SettingsKeys.PurchaseReturnTransactionPrefix, _purchaseReturnTransactionPrefix, Desc(SettingsKeys.PurchaseReturnTransactionPrefix));
			await UpdateSetting(SettingsKeys.KitchenIssueTransactionPrefix, _kitchenIssueTransactionPrefix, Desc(SettingsKeys.KitchenIssueTransactionPrefix));
			await UpdateSetting(SettingsKeys.KitchenProductionTransactionPrefix, _kitchenProductionTransactionPrefix, Desc(SettingsKeys.KitchenProductionTransactionPrefix));
			await UpdateSetting(SettingsKeys.RawMaterialStockAdjustmentTransactionPrefix, _rawMaterialStockAdjustmentTransactionPrefix, Desc(SettingsKeys.RawMaterialStockAdjustmentTransactionPrefix));
			await UpdateSetting(SettingsKeys.ProductStockAdjustmentTransactionPrefix, _productStockAdjustmentTransactionPrefix, Desc(SettingsKeys.ProductStockAdjustmentTransactionPrefix));
			await UpdateSetting(SettingsKeys.SaleTransactionPrefix, _saleTransactionPrefix, Desc(SettingsKeys.SaleTransactionPrefix));
			await UpdateSetting(SettingsKeys.SaleReturnTransactionPrefix, _saleReturnTransactionPrefix, Desc(SettingsKeys.SaleReturnTransactionPrefix));
			await UpdateSetting(SettingsKeys.StockTransferTransactionPrefix, _stockTransferTransactionPrefix, Desc(SettingsKeys.StockTransferTransactionPrefix));
			await UpdateSetting(SettingsKeys.OrderTransactionPrefix, _orderTransactionPrefix, Desc(SettingsKeys.OrderTransactionPrefix));
			await UpdateSetting(SettingsKeys.BillTransactionPrefix, _billTransactionPrefix, Desc(SettingsKeys.BillTransactionPrefix));

			// Vouchers
			await UpdateSetting(SettingsKeys.PurchaseVoucherId, _purchaseVoucherId, Desc(SettingsKeys.PurchaseVoucherId));
			await UpdateSetting(SettingsKeys.PurchaseReturnVoucherId, _purchaseReturnVoucherId, Desc(SettingsKeys.PurchaseReturnVoucherId));
			await UpdateSetting(SettingsKeys.SaleVoucherId, _saleVoucherId, Desc(SettingsKeys.SaleVoucherId));
			await UpdateSetting(SettingsKeys.SaleReturnVoucherId, _saleReturnVoucherId, Desc(SettingsKeys.SaleReturnVoucherId));
			await UpdateSetting(SettingsKeys.StockTransferVoucherId, _stockTransferVoucherId, Desc(SettingsKeys.StockTransferVoucherId));
			await UpdateSetting(SettingsKeys.BillVoucherId, _billVoucherId, Desc(SettingsKeys.BillVoucherId));
			await UpdateSetting(SettingsKeys.BillDayCloseVoucherId, _billDayCloseVoucherId, Desc(SettingsKeys.BillDayCloseVoucherId));
			await UpdateSetting(SettingsKeys.SaleDayCloseVoucherId, _saleDayCloseVoucherId, Desc(SettingsKeys.SaleDayCloseVoucherId));
			await UpdateSetting(SettingsKeys.DefaultSelectedVoucherId, _defaultSelectedVoucherId, Desc(SettingsKeys.DefaultSelectedVoucherId));

			// Ledgers
			await UpdateSetting(SettingsKeys.PurchaseLedgerId, _purchaseLedgerId, Desc(SettingsKeys.PurchaseLedgerId));
			await UpdateSetting(SettingsKeys.SaleLedgerId, _saleLedgerId, Desc(SettingsKeys.SaleLedgerId));
			await UpdateSetting(SettingsKeys.StockTransferLedgerId, _stockTransferLedgerId, Desc(SettingsKeys.StockTransferLedgerId));
			await UpdateSetting(SettingsKeys.BillLedgerId, _billLedgerId, Desc(SettingsKeys.BillLedgerId));
			await UpdateSetting(SettingsKeys.CashLedgerId, _cashLedgerId, Desc(SettingsKeys.CashLedgerId));
			await UpdateSetting(SettingsKeys.CashSalesLedgerId, _cashSalesLedgerId, Desc(SettingsKeys.CashSalesLedgerId));
			await UpdateSetting(SettingsKeys.GSTLedgerId, _gstLedgerId, Desc(SettingsKeys.GSTLedgerId));

			// Bank Reconciliation
			await UpdateSetting(SettingsKeys.BankAccountTypeId, _bankAccountTypeId, Desc(SettingsKeys.BankAccountTypeId));

			// Purchase Behavior
			await UpdateSetting(SettingsKeys.UpdateItemMasterRateOnPurchase, _updateItemMasterRateOnPurchase.ToString(), Desc(SettingsKeys.UpdateItemMasterRateOnPurchase));
			await UpdateSetting(SettingsKeys.UpdateItemMasterUOMOnPurchase, _updateItemMasterUOMOnPurchase.ToString(), Desc(SettingsKeys.UpdateItemMasterUOMOnPurchase));

			// Report Settings
			await UpdateSetting(SettingsKeys.AutoRefreshReportTimer, _autoRefreshReportTimer.ToString(), Desc(SettingsKeys.AutoRefreshReportTimer));
			await UpdateSetting(SettingsKeys.ReportWarningDays, _reportWarningDays.ToString(), Desc(SettingsKeys.ReportWarningDays));

			// Notification Settings
			await UpdateSetting(SettingsKeys.NotificationEmail, _notificationEmail, Desc(SettingsKeys.NotificationEmail));

			await _toastNotification.ShowAsync("Saved", "Settings saved successfully.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to save settings: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private static async Task UpdateSetting(string key, string value, string description)
	{
		await SettingsData.UpdateSettings(new SettingsModel
		{
			Key = key,
			Value = value ?? string.Empty,
			Description = description
		});
	}

	#endregion

	#region Reset Settings

	private async Task ShowResetConfirmation() =>
		await ShowConfirmation("Reset", "Are you sure you want to restore all settings to their default values?", ResetSettings);

	private async Task ResetSettings()
	{
		try
		{
			_isProcessing = true;
			StateHasChanged();

			await _toastNotification.ShowAsync("Resetting", "Restoring default settings...", ToastType.Info);
			await SettingsData.ResetSettings();
			await LoadData();
			await _toastNotification.ShowAsync("Reset", "Settings restored to defaults.", ToastType.Success);
		}
		catch (Exception ex)
		{
			await _toastNotification.ShowAsync("Error", $"Failed to reset settings: {ex.Message}", ToastType.Error);
		}
		finally
		{
			_isProcessing = false;
			StateHasChanged();
		}
	}

	private async Task ShowConfirmation(string title, string message, Func<Task> action)
	{
		_confirmTitle = title;
		_confirmMessage = message;
		_confirmAction = action;
		StateHasChanged();
		await _confirmationDialog.ShowAsync();
	}

	private async Task OnConfirmed()
	{
		await _confirmationDialog.HideAsync();
		if (_confirmAction is not null)
			await _confirmAction();
		_confirmAction = null;
	}

	private async Task OnCancelled()
	{
		_confirmAction = null;
		await _confirmationDialog.HideAsync();
	}

	#endregion
}

using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Models.Accounts.FinancialAccounting;

public sealed record FinancialAccountingSaveRequest(
	FinancialAccountingModel Accounting,
	List<FinancialAccountingLedgerModel> Ledgers,
	bool Recover);

public sealed record FinancialAccountingReportRequest(
	IEnumerable<FinancialAccountingOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	CompanyModel Company,
	VoucherModel Voucher);

public sealed record FinancialAccountingLedgerReportRequest(
	IEnumerable<FinancialAccountingLedgerOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	CompanyModel Company,
	LedgerModel Ledger,
	TrialBalanceModel TrialBalance);

using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Models.Accounts.FinancialAccounting;

public sealed record TrialBalanceReportRequest(
	IEnumerable<TrialBalanceModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	CompanyModel Company,
	GroupModel Group,
	AccountTypeModel AccountType);

using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Models.Store.Sale;

public sealed record OutletSummaryReportRequest(
	IEnumerable<OutletSummaryModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	CompanyModel Company);

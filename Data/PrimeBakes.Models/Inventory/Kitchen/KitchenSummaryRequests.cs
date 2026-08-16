using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Models.Inventory.Kitchen;

public sealed record KitchenSummaryReportRequest(
	IEnumerable<KitchenSummaryModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	CompanyModel Company);

using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Models.Inventory.Kitchen;

public sealed record KitchenIssueSaveRequest(
	KitchenIssueModel KitchenIssue,
	List<KitchenIssueDetailModel> Details,
	bool Recover);

public sealed record KitchenIssueReportRequest(
	IEnumerable<KitchenIssueOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	KitchenModel Kitchen,
	CompanyModel Company);

public sealed record KitchenIssueItemReportRequest(
	IEnumerable<KitchenIssueItemOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	RawMaterialModel RawMaterial,
	RawMaterialCategoryModel RawMaterialCategory,
	KitchenModel Kitchen,
	CompanyModel Company);

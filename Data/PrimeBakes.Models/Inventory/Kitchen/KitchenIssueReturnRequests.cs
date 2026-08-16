using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Models.Inventory.Kitchen;

public sealed record KitchenIssueReturnSaveRequest(
	KitchenIssueReturnModel KitchenIssueReturn,
	List<KitchenIssueReturnDetailModel> Details,
	bool Recover);

public sealed record KitchenIssueReturnReportRequest(
	IEnumerable<KitchenIssueReturnOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	KitchenModel Kitchen,
	CompanyModel Company);

public sealed record KitchenIssueReturnItemReportRequest(
	IEnumerable<KitchenIssueReturnItemOverviewModel> Data,
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

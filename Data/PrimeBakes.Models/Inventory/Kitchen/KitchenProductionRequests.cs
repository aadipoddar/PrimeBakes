using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Models.Inventory.Kitchen;

public sealed record KitchenProductionSaveRequest(
	KitchenProductionModel KitchenProduction,
	List<KitchenProductionDetailModel> Details,
	bool Recover);

public sealed record KitchenProductionReportRequest(
	IEnumerable<KitchenProductionOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	KitchenModel Kitchen,
	CompanyModel Company);

public sealed record KitchenProductionItemReportRequest(
	IEnumerable<KitchenProductionItemOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	ProductModel Product,
	ProductCategoryModel ProductCategory,
	KitchenModel Kitchen,
	CompanyModel Company);

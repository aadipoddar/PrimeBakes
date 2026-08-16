using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Models.Inventory.Kitchen;

public sealed record KitchenProductionReturnSaveRequest(
	KitchenProductionReturnModel KitchenProductionReturn,
	List<KitchenProductionReturnDetailModel> Details,
	bool Recover);

public sealed record KitchenProductionReturnReportRequest(
	IEnumerable<KitchenProductionReturnOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	KitchenModel Kitchen,
	CompanyModel Company);

public sealed record KitchenProductionReturnItemReportRequest(
	IEnumerable<KitchenProductionReturnItemOverviewModel> Data,
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

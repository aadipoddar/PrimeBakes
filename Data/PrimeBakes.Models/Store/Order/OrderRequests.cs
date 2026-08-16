using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Models.Store.Order;

public sealed record OrderSaveRequest(
	OrderModel Order,
	List<OrderDetailModel> OrderDetails,
	bool Recover);

public sealed record OrderReportRequest(
	IEnumerable<OrderOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	CompanyModel Company,
	LocationModel Location);

public sealed record OrderItemReportRequest(
	IEnumerable<OrderItemOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	ProductModel Product,
	ProductCategoryModel ProductCategory,
	CompanyModel Company,
	LocationModel Location);

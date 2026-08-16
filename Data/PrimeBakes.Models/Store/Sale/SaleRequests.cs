using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Customer;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Models.Store.Sale;

public sealed record SaleSaveRequest(
	SaleModel Sale,
	List<SaleDetailModel> SaleDetails,
	CustomerModel Customer,
	bool Recover);

public sealed record SaleReportRequest(
	IEnumerable<SaleOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	LedgerModel Party,
	CompanyModel Company,
	LocationModel Location);

public sealed record SaleItemReportRequest(
	IEnumerable<SaleItemOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	ProductModel Product,
	ProductCategoryModel ProductCategory,
	CompanyModel Company,
	LocationModel Location,
	LedgerModel Party);

using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Customer;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Models.Store.Sale;

public sealed record SaleReturnSaveRequest(
	SaleReturnModel SaleReturn,
	List<SaleReturnDetailModel> SaleReturnDetails,
	CustomerModel Customer,
	bool Recover);

public sealed record SaleReturnReportRequest(
	IEnumerable<SaleReturnOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	LedgerModel Party,
	CompanyModel Company,
	LocationModel Location);

public sealed record SaleReturnItemReportRequest(
	IEnumerable<SaleReturnItemOverviewModel> Data,
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

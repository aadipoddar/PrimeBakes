using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Customer;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Models.Restaurant.Bill;

public sealed record BillSaveRequest(
	BillModel Bill,
	List<BillDetailModel> BillDetails,
	CustomerModel Customer,
	bool Recover);

public sealed record BillReportRequest(
	IEnumerable<BillOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	CompanyModel Company,
	LocationModel Location);

public sealed record BillItemReportRequest(
	IEnumerable<BillItemOverviewModel> Data,
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

using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Models.Store.StockTransfer;

public sealed record StockTransferSaveRequest(
	StockTransferModel StockTransfer,
	List<StockTransferDetailModel> StockTransferDetails,
	bool Recover);

public sealed record StockTransferReportRequest(
	IEnumerable<StockTransferOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	CompanyModel Company,
	LocationModel FromLocation,
	LocationModel ToLocation);

public sealed record StockTransferItemReportRequest(
	IEnumerable<StockTransferItemOverviewModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	bool ShowDeleted,
	bool ShowSummary,
	ProductModel Product,
	ProductCategoryModel ProductCategory,
	CompanyModel Company,
	LocationModel FromLocation,
	LocationModel ToLocation);

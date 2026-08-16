using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;

namespace PrimeBakes.Models.Inventory.Stock;

public sealed record ProductStockAdjustmentRequest(
	DateTime TransactionDateTime,
	int LocationId,
	List<ProductStockAdjustmentCartModel> Cart);

public sealed record ProductStockSummaryReportRequest(
	IEnumerable<ProductStockSummaryModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns,
	LocationModel Location);

public sealed record ProductStockDetailsReportRequest(
	IEnumerable<ProductStockDetailsModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	LocationModel Location);

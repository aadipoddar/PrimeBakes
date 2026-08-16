using PrimeBakes.Models.Exports;

namespace PrimeBakes.Models.Inventory.Stock;

public sealed record RawMaterialStockAdjustmentRequest(
	DateTime TransactionDateTime,
	List<RawMaterialStockAdjustmentCartModel> Cart);

public sealed record RawMaterialStockSummaryReportRequest(
	IEnumerable<RawMaterialStockSummaryModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns);

public sealed record RawMaterialStockDetailsReportRequest(
	IEnumerable<RawMaterialStockDetailsModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd);

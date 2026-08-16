using PrimeBakes.Models.Exports;

namespace PrimeBakes.Models.Operations.AuditTrail;

public sealed record AuditTrailReportRequest(
	IEnumerable<AuditTrailModel> Data,
	ReportExportType ExportType,
	DateOnly? DateRangeStart,
	DateOnly? DateRangeEnd,
	bool ShowAllColumns);

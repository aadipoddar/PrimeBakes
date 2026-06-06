using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Operations.AuditTrail;

public static class AuditTrailExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<AuditTrailModel> data,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = false)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(AuditTrailModel.TransactionDateTime)] = new() { DisplayName = "Date", Format = "dd-MMM-yyyy HH:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(AuditTrailModel.Action)] = new() { DisplayName = "Action", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(AuditTrailModel.TableName)] = new() { DisplayName = "Module", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(AuditTrailModel.RecordNo)] = new() { DisplayName = "Record No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(AuditTrailModel.RecordValue)] = new() { DisplayName = "Changes", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(AuditTrailModel.CreatedByName)] = new() { DisplayName = "User", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(AuditTrailModel.CreatedFromPlatform)] = new() { DisplayName = "Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showAllColumns)
		{
			columnOrder =
			[
				nameof(AuditTrailModel.TransactionDateTime),
				nameof(AuditTrailModel.Action),
				nameof(AuditTrailModel.TableName),
				nameof(AuditTrailModel.RecordNo),
				nameof(AuditTrailModel.RecordValue),
				nameof(AuditTrailModel.CreatedByName),
				nameof(AuditTrailModel.CreatedFromPlatform),
			];
		}
		else
		{
			columnOrder =
			[
				nameof(AuditTrailModel.TransactionDateTime),
				nameof(AuditTrailModel.Action),
				nameof(AuditTrailModel.TableName),
				nameof(AuditTrailModel.RecordNo),
				nameof(AuditTrailModel.CreatedByName),
				nameof(AuditTrailModel.CreatedFromPlatform),
			];
		}

		string fileName = "AUDIT_TRAIL";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				data,
				"AUDIT TRAIL",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns
			);
			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				data,
				"AUDIT TRAIL",
				"Audit Trail",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder
			);
			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}

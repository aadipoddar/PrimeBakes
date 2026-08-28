using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;

namespace PrimeBakes.Exports.Operations.AuditTrail;

public static class AuditTrailExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<AuditTrailModel> data,
		DateTime currentDateTime,
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
			[nameof(AuditTrailModel.CreatedFormFactor)] = new() { DisplayName = "Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(AuditTrailModel.CreatedPlatform)] = new() { DisplayName = "Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(AuditTrailModel.CreatedLatitude)] = new() { DisplayName = "Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(AuditTrailModel.CreatedLongitude)] = new() { DisplayName = "Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(AuditTrailModel.CreatedUserOffset)] = new() { DisplayName = "Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
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
				nameof(AuditTrailModel.CreatedFormFactor),
				nameof(AuditTrailModel.CreatedPlatform),
				nameof(AuditTrailModel.CreatedLatitude),
				nameof(AuditTrailModel.CreatedLongitude),
				nameof(AuditTrailModel.CreatedUserOffset),
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
				nameof(AuditTrailModel.CreatedFormFactor),
				nameof(AuditTrailModel.CreatedPlatform),
				nameof(AuditTrailModel.CreatedLatitude),
				nameof(AuditTrailModel.CreatedLongitude),
				nameof(AuditTrailModel.CreatedUserOffset),
			];
		}

		string fileName = "AUDIT_TRAIL";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				data,
				"AUDIT TRAIL",
				currentDateTime,
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
			var stream = ExcelReportExportUtil.ExportToExcel(
				data,
				"AUDIT TRAIL",
				"Audit Trail",
				currentDateTime,
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

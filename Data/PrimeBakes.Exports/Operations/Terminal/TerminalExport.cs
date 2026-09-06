using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Terminal;

namespace PrimeBakes.Exports.Operations.Terminal;

public static class TerminalExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<TerminalOverviewModel> data,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(TerminalOverviewModel.TerminalNo)] = new() { DisplayName = "Terminal No", Format = "#,##0", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(TerminalOverviewModel.MachineName)] = new() { DisplayName = "Machine Name", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TerminalOverviewModel.MachineId)] = new() { DisplayName = "Machine Id", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TerminalOverviewModel.UserName)] = new() { DisplayName = "User", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(TerminalOverviewModel.LastSyncedAt)] = new() { DisplayName = "Last Synced", Format = "dd-MMM-yyyy HH:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder =
		[
			nameof(TerminalOverviewModel.TerminalNo),
			nameof(TerminalOverviewModel.MachineName),
			nameof(TerminalOverviewModel.MachineId),
			nameof(TerminalOverviewModel.UserName),
			nameof(TerminalOverviewModel.LastSyncedAt),
		];

		string fileName = "TERMINAL";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				data,
				"TERMINAL",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: true
			);
			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = ExcelReportExportUtil.ExportToExcel(
				data,
				"TERMINAL",
				"Terminal",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder
			);
			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}

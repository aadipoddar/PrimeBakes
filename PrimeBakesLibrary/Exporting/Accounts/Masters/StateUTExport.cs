using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class StateUTExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(
		IEnumerable<StateUTModel> stateUTData,
		ReportExportType exportType)
	{
		var enrichedData = stateUTData.Select(stateUT => new
		{
			stateUT.Id,
			stateUT.Name,
			stateUT.Remarks,
			UnionTerritory = stateUT.UnionTerritory ? "Yes" : "No",
			Status = stateUT.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(StateUTModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StateUTModel.Name)] = new() { DisplayName = "State/UT Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(StateUTModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(StateUTModel.UnionTerritory)] = new() { DisplayName = "Union Territory", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StateUTModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(StateUTModel.Id),
			nameof(StateUTModel.Name),
			nameof(StateUTModel.Remarks),
			nameof(StateUTModel.UnionTerritory),
			nameof(StateUTModel.Status)
		];

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"StateUT_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"STATE & UNION TERRITORY MASTER",
				null,
				null,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: false
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"STATE & UNION TERRITORY",
				"State UT Data",
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

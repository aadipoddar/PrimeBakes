using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Exports.Accounts.Masters;

public static class GroupExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<GroupModel> groupData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = groupData.Select(group => new
		{
			group.Id,
			group.Name,
			group.Nature,
			group.Remarks,
			Status = group.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(GroupModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(GroupModel.Name)] = new() { DisplayName = "Group Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(GroupModel.Nature)] = new() { DisplayName = "Nature", Alignment = CellAlignment.Left },
			[nameof(GroupModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(GroupModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(GroupModel.Id),
			nameof(GroupModel.Name),
			nameof(GroupModel.Nature),
			nameof(GroupModel.Remarks),
			nameof(GroupModel.Status)
		];

		var fileName = $"Group_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"GROUP MASTER",
				currentDateTime,
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
			var stream = ExcelReportExportUtil.ExportToExcel(
				enrichedData,
				"GROUP",
				"Group Data",
				currentDateTime,
				null,
				null,
				columnSettings,
				columnOrder
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

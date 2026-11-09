using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Exports.Store.Customer;

public static class OnlinePartnerExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<OnlinePartnerModel> onlinePartnerData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = onlinePartnerData.Select(onlinePartner => new
		{
			onlinePartner.Id,
			onlinePartner.Name,
			onlinePartner.Remarks,
			Status = onlinePartner.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(OnlinePartnerModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OnlinePartnerModel.Name)] = new() { DisplayName = "Online Partner Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(OnlinePartnerModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(OnlinePartnerModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(OnlinePartnerModel.Id),
			nameof(OnlinePartnerModel.Name),
			nameof(OnlinePartnerModel.Remarks),
			nameof(OnlinePartnerModel.Status)
		];

		var fileName = $"OnlinePartner_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"ONLINE PARTNER MASTER",
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
				"ONLINE PARTNER",
				"Online Partner Data",
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

using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;

namespace PrimeBakes.Exports.Operations.Location;

public static class LocationExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<LocationModel> locationData,
		IEnumerable<LedgerModel> ledgers,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = locationData.Select(location => new
		{
			location.Id,
			location.Name,
			location.Code,
			location.Discount,
			location.UseLocationRateOnSale,
			LedgerName = ledgers.FirstOrDefault(l => l.Id == location.LedgerId)?.Name ?? "N/A",
			location.Remarks,
			Status = location.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(LocationModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(LocationModel.Name)] = new() { DisplayName = "Location Name", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(LocationModel.Code)] = new() { DisplayName = "Code", Alignment = CellAlignment.Center, IsRequired = true },
			[nameof(LocationModel.Discount)] = new() { DisplayName = "Discount %", Alignment = CellAlignment.Right, Format = "#,##0.00", IncludeInTotal = false },
			[nameof(LocationModel.UseLocationRateOnSale)] = new() { DisplayName = "Use Location Rate On Sale", Alignment = CellAlignment.Center, IncludeInTotal = false },
			["LedgerName"] = new() { DisplayName = "Ledger", Alignment = CellAlignment.Left, IsRequired = true },
			[nameof(LocationModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(LocationModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(LocationModel.Id),
			nameof(LocationModel.Name),
			nameof(LocationModel.Code),
			nameof(LocationModel.Discount),
			nameof(LocationModel.UseLocationRateOnSale),
			"LedgerName",
			nameof(LocationModel.Remarks),
			nameof(LocationModel.Status)
		];

		var fileName = $"Location_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"LOCATION MASTER",
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
				"LOCATION MASTER",
				"Location Data",
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

using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Exports.Accounts.Masters;

public static class FinancialYearExport
{
	public static (MemoryStream stream, string fileName) ExportMaster(
		IEnumerable<FinancialYearModel> financialYearData,
		DateTime currentDateTime,
		ReportExportType exportType)
	{
		var enrichedData = financialYearData.Select(fy => new
		{
			fy.Id,
			StartDate = fy.StartDate.ToString("dd-MMM-yyyy"),
			EndDate = fy.EndDate.ToString("dd-MMM-yyyy"),
			fy.YearNo,
			fy.Remarks,
			Locked = fy.Locked ? "Yes" : "No",
			Status = fy.Status ? "Active" : "Deleted"
		});

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(FinancialYearModel.Id)] = new() { DisplayName = "ID", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialYearModel.StartDate)] = new() { DisplayName = "Start Date", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialYearModel.EndDate)] = new() { DisplayName = "End Date", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialYearModel.YearNo)] = new() { DisplayName = "Year No", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialYearModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left },
			[nameof(FinancialYearModel.Locked)] = new() { DisplayName = "Locked", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(FinancialYearModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder =
		[
			nameof(FinancialYearModel.Id),
			nameof(FinancialYearModel.StartDate),
			nameof(FinancialYearModel.EndDate),
			nameof(FinancialYearModel.YearNo),
			nameof(FinancialYearModel.Remarks),
			nameof(FinancialYearModel.Locked),
			nameof(FinancialYearModel.Status)
		];

		var fileName = $"FinancialYear_Master_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				enrichedData,
				"FINANCIAL YEAR MASTER",
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
				"FINANCIAL YEAR",
				"Financial Year Data",
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

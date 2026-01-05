using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;

namespace PrimeBakesLibrary.Exporting.Accounts.Masters;

public static class FinancialYearExcelExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportMaster(IEnumerable<FinancialYearModel> financialYearData)
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

		var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
		{
			[nameof(FinancialYearModel.Id)] = new() { DisplayName = "ID", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			[nameof(FinancialYearModel.StartDate)] = new() { DisplayName = "Start Date", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(FinancialYearModel.EndDate)] = new() { DisplayName = "End Date", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			[nameof(FinancialYearModel.YearNo)] = new() { DisplayName = "Year No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			[nameof(FinancialYearModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft },

			[nameof(FinancialYearModel.Locked)] = new() { DisplayName = "Locked", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(FinancialYearModel.Status)] = new() { DisplayName = "Status", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
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

		var stream = await ExcelReportExportUtil.ExportToExcel(
			enrichedData,
			"FINANCIAL YEAR",
			"Financial Year Data",
			null,
			null,
			columnSettings,
			columnOrder
		);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var fileName = $"FinancialYear_Master_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
		return (stream, fileName);
	}
}

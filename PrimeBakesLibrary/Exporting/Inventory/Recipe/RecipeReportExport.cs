using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory;

namespace PrimeBakesLibrary.Exporting.Inventory.Recipe;

public static class RecipeReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<RecipeOverviewModel> data,
		ReportExportType exportType)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(RecipeOverviewModel.ProductName)] = new() { DisplayName = "Product", Alignment = CellAlignment.Left, IncludeInTotal = false, ExcelWidth = 30 },
			[nameof(RecipeOverviewModel.Quantity)] = new() { DisplayName = "Recipe Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RecipeOverviewModel.Deduct)] = new() { DisplayName = "Deduct on Sale", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RecipeOverviewModel.ItemCount)] = new() { DisplayName = "Ingredients", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RecipeOverviewModel.TotalCost)] = new() { DisplayName = "Total Cost", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, IsGrandTotal = true },
			[nameof(RecipeOverviewModel.PerUnitCost)] = new() { DisplayName = "Per Unit Cost", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
		};

		var columnOrder = new List<string>
		{
			nameof(RecipeOverviewModel.ProductName),
			nameof(RecipeOverviewModel.Quantity),
			nameof(RecipeOverviewModel.Deduct),
			nameof(RecipeOverviewModel.ItemCount),
			nameof(RecipeOverviewModel.TotalCost),
			nameof(RecipeOverviewModel.PerUnitCost),
		};

		string fileName = $"RECIPE_REPORT_{DateTime.Now:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(data, "Recipe Report", columnSettings: columnSettings, columnOrder: columnOrder, useLandscape: true);
			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(data, "Recipe Report", "Recipes", columnSettings: columnSettings, columnOrder: columnOrder);
			return (stream, fileName + ".xlsx");
		}
	}
}

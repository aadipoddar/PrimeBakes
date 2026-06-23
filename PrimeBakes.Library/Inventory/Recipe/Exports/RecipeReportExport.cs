using PrimeBakes.Library.Inventory.Recipe.Models;
using PrimeBakes.Library.Utils.Exports;

namespace PrimeBakes.Library.Inventory.Recipe.Exports;

public static class RecipeReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<RecipeOverviewModel> data,
		ReportExportType exportType,
		DateOnly? effectiveDate = null,
		DateOnly? costAsOnDate = null,
		string deduct = null)
	{
		var headerMetadata = new Dictionary<string, string>
		{
			["Effective Date"] = effectiveDate?.ToString("dd-MMM-yyyy"),
			["Cost As On Date"] = costAsOnDate?.ToString("dd-MMM-yyyy"),
			["Deduct on Sale"] = deduct
		};

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(RecipeOverviewModel.ProductName)] = new() { DisplayName = "Product", Alignment = CellAlignment.Left, IncludeInTotal = false, ExcelWidth = 30 },
			[nameof(RecipeOverviewModel.FromDate)] = new() { DisplayName = "Effective Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RecipeOverviewModel.Quantity)] = new() { DisplayName = "Recipe Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RecipeOverviewModel.Deduct)] = new() { DisplayName = "Deduct on Sale", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RecipeOverviewModel.ItemCount)] = new() { DisplayName = "Ingredients", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RecipeOverviewModel.TotalCost)] = new() { DisplayName = "Total Cost", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, IsGrandTotal = true },
			[nameof(RecipeOverviewModel.PerUnitCost)] = new() { DisplayName = "Per Unit Cost", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
		};

		var columnOrder = new List<string>
		{
			nameof(RecipeOverviewModel.ProductName),
			nameof(RecipeOverviewModel.FromDate),
			nameof(RecipeOverviewModel.Quantity),
			nameof(RecipeOverviewModel.Deduct),
			nameof(RecipeOverviewModel.ItemCount),
			nameof(RecipeOverviewModel.TotalCost),
			nameof(RecipeOverviewModel.PerUnitCost),
		};

		string fileName = $"RECIPE_REPORT_{DateTime.Now:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				data,
				"Recipe Report",
				columnSettings: columnSettings,
				columnOrder: columnOrder,
				useLandscape: true,
				headerMetadata: headerMetadata);
			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				data,
				"Recipe Report",
				"Recipes",
				columnSettings: columnSettings,
				columnOrder: columnOrder,
				headerMetadata: headerMetadata);
			return (stream, fileName + ".xlsx");
		}
	}

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<RecipeItemOverviewModel> data,
		ReportExportType exportType,
		DateOnly? effectiveDate = null,
		DateOnly? costAsOnDate = null,
		string deduct = null,
		string rawMaterial = null,
		string category = null,
		string product = null)
	{
		var headerMetadata = new Dictionary<string, string>
		{
			["Effective Date"] = effectiveDate?.ToString("dd-MMM-yyyy"),
			["Cost As On Date"] = costAsOnDate?.ToString("dd-MMM-yyyy"),
			["Deduct on Sale"] = deduct,
			["Raw Material"] = rawMaterial,
			["Category"] = category,
			["Recipe (Product)"] = product
		};

		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(RecipeItemOverviewModel.ItemName)] = new() { DisplayName = "Raw Material", Alignment = CellAlignment.Left, IncludeInTotal = false, ExcelWidth = 30 },
			[nameof(RecipeItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RecipeItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(RecipeItemOverviewModel.UnitOfMeasurement)] = new() { DisplayName = "UOM", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RecipeItemOverviewModel.Quantity)] = new() { DisplayName = "Quantity", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(RecipeItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RecipeItemOverviewModel.Amount)] = new() { DisplayName = "Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, IsGrandTotal = true },
			[nameof(RecipeItemOverviewModel.PerUnit)] = new() { DisplayName = "Per Unit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RecipeItemOverviewModel.ProductName)] = new() { DisplayName = "Recipe (Product)", Alignment = CellAlignment.Left, IncludeInTotal = false, ExcelWidth = 30 },
			[nameof(RecipeItemOverviewModel.RecipeQuantity)] = new() { DisplayName = "Recipe Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(RecipeItemOverviewModel.Deduct)] = new() { DisplayName = "Deduct on Sale", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(RecipeItemOverviewModel.FromDate)] = new() { DisplayName = "Effective Date", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder =
		[
			nameof(RecipeItemOverviewModel.ItemName),
			nameof(RecipeItemOverviewModel.ItemCode),
			nameof(RecipeItemOverviewModel.ItemCategoryName),
			nameof(RecipeItemOverviewModel.UnitOfMeasurement),
			nameof(RecipeItemOverviewModel.Quantity),
			nameof(RecipeItemOverviewModel.Rate),
			nameof(RecipeItemOverviewModel.Amount),
			nameof(RecipeItemOverviewModel.PerUnit),
			nameof(RecipeItemOverviewModel.ProductName),
			nameof(RecipeItemOverviewModel.RecipeQuantity),
			nameof(RecipeItemOverviewModel.Deduct),
			nameof(RecipeItemOverviewModel.FromDate)
		];

		string fileName = $"RECIPE_ITEM_REPORT_{DateTime.Now:yyyyMMdd_HHmmss}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				data,
				"Recipe Item Report",
				columnSettings: columnSettings,
				columnOrder: columnOrder,
				useLandscape: true,
				headerMetadata: headerMetadata);
			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				data,
				"Recipe Item Report",
				"Recipe Items",
				columnSettings: columnSettings,
				columnOrder: columnOrder,
				headerMetadata: headerMetadata);
			return (stream, fileName + ".xlsx");
		}
	}
}

using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Inventory.Kitchen.Models;
using PrimeBakesLibrary.Inventory.RawMaterial.Models;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Inventory.Kitchen.Exports;

public static class KitchenIssueReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<KitchenIssueOverviewModel> kitchenIssueData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		KitchenModel kitchen = null,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(KitchenIssueOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenIssueOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
		{
			columnOrder =
			[
				nameof(KitchenIssueOverviewModel.KitchenName),
				nameof(KitchenIssueOverviewModel.TotalItems),
				nameof(KitchenIssueOverviewModel.TotalQuantity),
				nameof(KitchenIssueOverviewModel.TotalAmount)
			];

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenIssueOverviewModel.KitchenName));
		}

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(KitchenIssueOverviewModel.TransactionNo),
				nameof(KitchenIssueOverviewModel.CompanyName),
				nameof(KitchenIssueOverviewModel.TransactionDateTime),
				nameof(KitchenIssueOverviewModel.FinancialYear),
				nameof(KitchenIssueOverviewModel.KitchenName),
				nameof(KitchenIssueOverviewModel.TotalItems),
				nameof(KitchenIssueOverviewModel.TotalQuantity),
				nameof(KitchenIssueOverviewModel.TotalAmount),
				nameof(KitchenIssueOverviewModel.Remarks),
				nameof(KitchenIssueOverviewModel.CreatedByName),
				nameof(KitchenIssueOverviewModel.CreatedAt),
				nameof(KitchenIssueOverviewModel.CreatedFromPlatform),
				nameof(KitchenIssueOverviewModel.LastModifiedByUserName),
				nameof(KitchenIssueOverviewModel.LastModifiedAt),
				nameof(KitchenIssueOverviewModel.LastModifiedFromPlatform),
				nameof(KitchenIssueOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenIssueOverviewModel.Status));
		}

		else
		{
			columnOrder =
			[
				nameof(KitchenIssueOverviewModel.TransactionNo),
				nameof(KitchenIssueOverviewModel.TransactionDateTime),
				nameof(KitchenIssueOverviewModel.KitchenName),
				nameof(KitchenIssueOverviewModel.TotalQuantity),
				nameof(KitchenIssueOverviewModel.TotalAmount),
				nameof(KitchenIssueOverviewModel.Status)
			];

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenIssueOverviewModel.KitchenName));

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenIssueOverviewModel.Status));
		}

		string fileName = $"KITCHEN_ISSUE_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				kitchenIssueData,
				"KITCHEN ISSUE REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new() { ["Company"] = company?.Name ?? null, ["Kitchen"] = kitchen?.Name ?? null }
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				kitchenIssueData,
				"KITCHEN ISSUE REPORT",
				"Kitchen Issue Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Company"] = company?.Name ?? null, ["Kitchen"] = kitchen?.Name ?? null }
			);

			return (stream, fileName + ".xlsx");
		}
	}

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<KitchenIssueItemOverviewModel> kitchenIssueItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		RawMaterialModel rawMaterial = null,
		RawMaterialCategoryModel rawMaterialCategory = null,
		KitchenModel kitchen = null,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(KitchenIssueItemOverviewModel.ItemName)] = new() { DisplayName = "Raw Material", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenIssueItemOverviewModel.UnitOfMeasurement)] = new() { DisplayName = "UOM", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(KitchenIssueItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenIssueItemOverviewModel.ItemRemarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.KitchenIssueRemarks)] = new() { DisplayName = "Issue Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.TotalItems)] = new() { DisplayName = "Total Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.TotalQuantity)] = new() { DisplayName = "Total Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.TotalAmount)] = new() { DisplayName = "Total Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(KitchenIssueItemOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueItemOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(KitchenIssueItemOverviewModel.ItemName),
				nameof(KitchenIssueItemOverviewModel.ItemCode),
				nameof(KitchenIssueItemOverviewModel.ItemCategoryName),
				nameof(KitchenIssueItemOverviewModel.Quantity),
				nameof(KitchenIssueItemOverviewModel.Total)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(KitchenIssueItemOverviewModel.ItemName),
				nameof(KitchenIssueItemOverviewModel.ItemCode),
				nameof(KitchenIssueItemOverviewModel.ItemCategoryName),
				nameof(KitchenIssueItemOverviewModel.Quantity),
				nameof(KitchenIssueItemOverviewModel.UnitOfMeasurement),
				nameof(KitchenIssueItemOverviewModel.Rate),
				nameof(KitchenIssueItemOverviewModel.Total),
				nameof(KitchenIssueItemOverviewModel.ItemRemarks),
				nameof(KitchenIssueItemOverviewModel.TransactionNo),
				nameof(KitchenIssueItemOverviewModel.CompanyName),
				nameof(KitchenIssueItemOverviewModel.TransactionDateTime),
				nameof(KitchenIssueItemOverviewModel.FinancialYear),
				nameof(KitchenIssueItemOverviewModel.KitchenName),
				nameof(KitchenIssueItemOverviewModel.KitchenIssueRemarks),
				nameof(KitchenIssueItemOverviewModel.TotalItems),
				nameof(KitchenIssueItemOverviewModel.TotalQuantity),
				nameof(KitchenIssueItemOverviewModel.TotalAmount),
				nameof(KitchenIssueItemOverviewModel.CreatedByName),
				nameof(KitchenIssueItemOverviewModel.CreatedAt),
				nameof(KitchenIssueItemOverviewModel.CreatedFromPlatform),
				nameof(KitchenIssueItemOverviewModel.LastModifiedByUserName),
				nameof(KitchenIssueItemOverviewModel.LastModifiedAt),
				nameof(KitchenIssueItemOverviewModel.LastModifiedFromPlatform),
				nameof(KitchenIssueItemOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenIssueItemOverviewModel.MasterStatus));
		}

		else
		{
			columnOrder =
			[
				nameof(KitchenIssueItemOverviewModel.ItemName),
				nameof(KitchenIssueItemOverviewModel.ItemCode),
				nameof(KitchenIssueItemOverviewModel.Quantity),
				nameof(KitchenIssueItemOverviewModel.Rate),
				nameof(KitchenIssueItemOverviewModel.Total),
				nameof(KitchenIssueItemOverviewModel.TransactionNo),
				nameof(KitchenIssueItemOverviewModel.TransactionDateTime),
				nameof(KitchenIssueItemOverviewModel.KitchenName)
			];

			if (rawMaterial is not null)
				columnOrder.Remove(nameof(KitchenIssueItemOverviewModel.ItemName));

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenIssueItemOverviewModel.KitchenName));
		}

		string fileName = $"KITCHEN_ISSUE_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				kitchenIssueItemData,
				"KITCHEN ISSUE ITEM REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new()
				{
					["Raw Material"] = rawMaterial?.Name ?? null,
					["Raw Material Category"] = rawMaterialCategory?.Name ?? null,
					["Company"] = company?.Name ?? null,
					["Kitchen"] = kitchen?.Name ?? null
				}
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				kitchenIssueItemData,
				"KITCHEN ISSUE ITEM REPORT",
				"Kitchen Issue Item Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new()
				{
					["Raw Material"] = rawMaterial?.Name ?? null,
					["Raw Material Category"] = rawMaterialCategory?.Name ?? null,
					["Company"] = company?.Name ?? null,
					["Kitchen"] = kitchen?.Name ?? null
				}
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

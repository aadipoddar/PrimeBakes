using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Inventory.Kitchen.Models;
using PrimeBakes.Library.Store.Product.Models;
using PrimeBakes.Library.Utils.Exports;

namespace PrimeBakes.Library.Inventory.Kitchen.Exports;

public static class KitchenProductionReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<KitchenProductionOverviewModel> kitchenProductionData,
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
			[nameof(KitchenProductionOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenProductionOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
		{
			columnOrder =
			[
				nameof(KitchenProductionOverviewModel.KitchenName),
				nameof(KitchenProductionOverviewModel.TotalItems),
				nameof(KitchenProductionOverviewModel.TotalQuantity),
				nameof(KitchenProductionOverviewModel.TotalAmount)
			];

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenProductionOverviewModel.KitchenName));
		}

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(KitchenProductionOverviewModel.TransactionNo),
				nameof(KitchenProductionOverviewModel.CompanyName),
				nameof(KitchenProductionOverviewModel.TransactionDateTime),
				nameof(KitchenProductionOverviewModel.FinancialYear),
				nameof(KitchenProductionOverviewModel.KitchenName),
				nameof(KitchenProductionOverviewModel.TotalItems),
				nameof(KitchenProductionOverviewModel.TotalQuantity),
				nameof(KitchenProductionOverviewModel.TotalAmount),
				nameof(KitchenProductionOverviewModel.Remarks),
				nameof(KitchenProductionOverviewModel.CreatedByName),
				nameof(KitchenProductionOverviewModel.CreatedAt),
				nameof(KitchenProductionOverviewModel.CreatedFromPlatform),
				nameof(KitchenProductionOverviewModel.LastModifiedByUserName),
				nameof(KitchenProductionOverviewModel.LastModifiedAt),
				nameof(KitchenProductionOverviewModel.LastModifiedFromPlatform),
				nameof(KitchenProductionOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenProductionOverviewModel.Status));
		}

		else
		{
			columnOrder =
			[
				nameof(KitchenProductionOverviewModel.TransactionNo),
				nameof(KitchenProductionOverviewModel.TransactionDateTime),
				nameof(KitchenProductionOverviewModel.KitchenName),
				nameof(KitchenProductionOverviewModel.TotalQuantity),
				nameof(KitchenProductionOverviewModel.TotalAmount),
				nameof(KitchenProductionOverviewModel.Status)
			];

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenProductionOverviewModel.KitchenName));

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenProductionOverviewModel.Status));
		}

		string fileName = $"KITCHEN_PRODUCTION_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				kitchenProductionData,
				"KITCHEN PRODUCTION REPORT",
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
				kitchenProductionData,
				"KITCHEN PRODUCTION REPORT",
				"Kitchen Production Transactions",
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
		IEnumerable<KitchenProductionItemOverviewModel> kitchenProductionItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		ProductModel product = null,
		ProductCategoryModel productCategory = null,
		KitchenModel kitchen = null,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(KitchenProductionItemOverviewModel.ItemName)] = new() { DisplayName = "Product", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenProductionItemOverviewModel.ItemRemarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.KitchenProductionRemarks)] = new() { DisplayName = "Production Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.TotalItems)] = new() { DisplayName = "Total Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.TotalQuantity)] = new() { DisplayName = "Total Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.TotalAmount)] = new() { DisplayName = "Total Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(KitchenProductionItemOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionItemOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(KitchenProductionItemOverviewModel.ItemName),
				nameof(KitchenProductionItemOverviewModel.ItemCode),
				nameof(KitchenProductionItemOverviewModel.ItemCategoryName),
				nameof(KitchenProductionItemOverviewModel.Quantity),
				nameof(KitchenProductionItemOverviewModel.Total)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(KitchenProductionItemOverviewModel.ItemName),
				nameof(KitchenProductionItemOverviewModel.ItemCode),
				nameof(KitchenProductionItemOverviewModel.ItemCategoryName),
				nameof(KitchenProductionItemOverviewModel.Quantity),
				nameof(KitchenProductionItemOverviewModel.Rate),
				nameof(KitchenProductionItemOverviewModel.Total),
				nameof(KitchenProductionItemOverviewModel.ItemRemarks),
				nameof(KitchenProductionItemOverviewModel.TransactionNo),
				nameof(KitchenProductionItemOverviewModel.CompanyName),
				nameof(KitchenProductionItemOverviewModel.TransactionDateTime),
				nameof(KitchenProductionItemOverviewModel.FinancialYear),
				nameof(KitchenProductionItemOverviewModel.KitchenName),
				nameof(KitchenProductionItemOverviewModel.KitchenProductionRemarks),
				nameof(KitchenProductionItemOverviewModel.TotalItems),
				nameof(KitchenProductionItemOverviewModel.TotalQuantity),
				nameof(KitchenProductionItemOverviewModel.TotalAmount),
				nameof(KitchenProductionItemOverviewModel.CreatedByName),
				nameof(KitchenProductionItemOverviewModel.CreatedAt),
				nameof(KitchenProductionItemOverviewModel.CreatedFromPlatform),
				nameof(KitchenProductionItemOverviewModel.LastModifiedByUserName),
				nameof(KitchenProductionItemOverviewModel.LastModifiedAt),
				nameof(KitchenProductionItemOverviewModel.LastModifiedFromPlatform),
				nameof(KitchenProductionItemOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenProductionItemOverviewModel.MasterStatus));
		}

		else
		{
			columnOrder =
			[
				nameof(KitchenProductionItemOverviewModel.ItemName),
				nameof(KitchenProductionItemOverviewModel.ItemCode),
				nameof(KitchenProductionItemOverviewModel.TransactionNo),
				nameof(KitchenProductionItemOverviewModel.TransactionDateTime),
				nameof(KitchenProductionItemOverviewModel.KitchenName),
				nameof(KitchenProductionItemOverviewModel.Quantity),
				nameof(KitchenProductionItemOverviewModel.Rate),
				nameof(KitchenProductionItemOverviewModel.Total)
			];

			if (product is not null)
				columnOrder.Remove(nameof(KitchenProductionItemOverviewModel.ItemName));

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenProductionItemOverviewModel.KitchenName));
		}

		string fileName = $"KITCHEN_PRODUCTION_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				kitchenProductionItemData,
				"KITCHEN PRODUCTION ITEM REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new()
				{
					["Product"] = product?.Name ?? null,
					["Product Category"] = productCategory?.Name ?? null,
					["Company"] = company?.Name ?? null,
					["Kitchen"] = kitchen?.Name ?? null
				}
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				kitchenProductionItemData,
				"KITCHEN PRODUCTION ITEM REPORT",
				"Kitchen Production Item Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new()
				{
					["Product"] = product?.Name ?? null,
					["Product Category"] = productCategory?.Name ?? null,
					["Company"] = company?.Name ?? null,
					["Kitchen"] = kitchen?.Name ?? null
				}
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

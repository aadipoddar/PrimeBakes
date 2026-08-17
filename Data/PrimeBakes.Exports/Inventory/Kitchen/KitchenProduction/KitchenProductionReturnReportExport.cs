using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Exports.Inventory.Kitchen;

public static class KitchenProductionReturnReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<KitchenProductionReturnOverviewModel> kitchenProductionReturnData,
		DateTime currentDateTime,
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
			[nameof(KitchenProductionReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionReturnOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionReturnOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenProductionReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
		{
			columnOrder =
			[
				nameof(KitchenProductionReturnOverviewModel.KitchenName),
				nameof(KitchenProductionReturnOverviewModel.TotalItems),
				nameof(KitchenProductionReturnOverviewModel.TotalQuantity),
				nameof(KitchenProductionReturnOverviewModel.TotalAmount)
			];

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenProductionReturnOverviewModel.KitchenName));
		}

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(KitchenProductionReturnOverviewModel.TransactionNo),
				nameof(KitchenProductionReturnOverviewModel.CompanyName),
				nameof(KitchenProductionReturnOverviewModel.TransactionDateTime),
				nameof(KitchenProductionReturnOverviewModel.FinancialYear),
				nameof(KitchenProductionReturnOverviewModel.KitchenName),
				nameof(KitchenProductionReturnOverviewModel.TotalItems),
				nameof(KitchenProductionReturnOverviewModel.TotalQuantity),
				nameof(KitchenProductionReturnOverviewModel.TotalAmount),
				nameof(KitchenProductionReturnOverviewModel.Remarks),
				nameof(KitchenProductionReturnOverviewModel.CreatedByName),
				nameof(KitchenProductionReturnOverviewModel.CreatedAt),
				nameof(KitchenProductionReturnOverviewModel.CreatedFromPlatform),
				nameof(KitchenProductionReturnOverviewModel.LastModifiedByUserName),
				nameof(KitchenProductionReturnOverviewModel.LastModifiedAt),
				nameof(KitchenProductionReturnOverviewModel.LastModifiedFromPlatform),
				nameof(KitchenProductionReturnOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenProductionReturnOverviewModel.Status));
		}

		else
		{
			columnOrder =
			[
				nameof(KitchenProductionReturnOverviewModel.TransactionNo),
				nameof(KitchenProductionReturnOverviewModel.TransactionDateTime),
				nameof(KitchenProductionReturnOverviewModel.KitchenName),
				nameof(KitchenProductionReturnOverviewModel.TotalQuantity),
				nameof(KitchenProductionReturnOverviewModel.TotalAmount),
				nameof(KitchenProductionReturnOverviewModel.Status)
			];

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenProductionReturnOverviewModel.KitchenName));

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenProductionReturnOverviewModel.Status));
		}

		string fileName = $"KITCHEN_PRODUCTION_RETURN_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				kitchenProductionReturnData,
				"KITCHEN PRODUCTION RETURN REPORT",
				currentDateTime,
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
			var stream = ExcelReportExportUtil.ExportToExcel(
				kitchenProductionReturnData,
				"KITCHEN PRODUCTION RETURN REPORT",
				"Kitchen Production Return Transactions",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Company"] = company?.Name ?? null, ["Kitchen"] = kitchen?.Name ?? null }
			);

			return (stream, fileName + ".xlsx");
		}
	}

	public static (MemoryStream stream, string fileName) ExportItemReport(
		IEnumerable<KitchenProductionReturnItemOverviewModel> kitchenProductionReturnItemData,
		DateTime currentDateTime,
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
			[nameof(KitchenProductionReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Product", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenProductionReturnItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenProductionReturnItemOverviewModel.ItemRemarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.KitchenProductionReturnRemarks)] = new() { DisplayName = "Return Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.TotalItems)] = new() { DisplayName = "Total Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.TotalQuantity)] = new() { DisplayName = "Total Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.TotalAmount)] = new() { DisplayName = "Total Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(KitchenProductionReturnItemOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenProductionReturnItemOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(KitchenProductionReturnItemOverviewModel.ItemName),
				nameof(KitchenProductionReturnItemOverviewModel.ItemCode),
				nameof(KitchenProductionReturnItemOverviewModel.ItemCategoryName),
				nameof(KitchenProductionReturnItemOverviewModel.Quantity),
				nameof(KitchenProductionReturnItemOverviewModel.Total)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(KitchenProductionReturnItemOverviewModel.ItemName),
				nameof(KitchenProductionReturnItemOverviewModel.ItemCode),
				nameof(KitchenProductionReturnItemOverviewModel.ItemCategoryName),
				nameof(KitchenProductionReturnItemOverviewModel.Quantity),
				nameof(KitchenProductionReturnItemOverviewModel.Rate),
				nameof(KitchenProductionReturnItemOverviewModel.Total),
				nameof(KitchenProductionReturnItemOverviewModel.ItemRemarks),
				nameof(KitchenProductionReturnItemOverviewModel.TransactionNo),
				nameof(KitchenProductionReturnItemOverviewModel.CompanyName),
				nameof(KitchenProductionReturnItemOverviewModel.TransactionDateTime),
				nameof(KitchenProductionReturnItemOverviewModel.FinancialYear),
				nameof(KitchenProductionReturnItemOverviewModel.KitchenName),
				nameof(KitchenProductionReturnItemOverviewModel.KitchenProductionReturnRemarks),
				nameof(KitchenProductionReturnItemOverviewModel.TotalItems),
				nameof(KitchenProductionReturnItemOverviewModel.TotalQuantity),
				nameof(KitchenProductionReturnItemOverviewModel.TotalAmount),
				nameof(KitchenProductionReturnItemOverviewModel.CreatedByName),
				nameof(KitchenProductionReturnItemOverviewModel.CreatedAt),
				nameof(KitchenProductionReturnItemOverviewModel.CreatedFromPlatform),
				nameof(KitchenProductionReturnItemOverviewModel.LastModifiedByUserName),
				nameof(KitchenProductionReturnItemOverviewModel.LastModifiedAt),
				nameof(KitchenProductionReturnItemOverviewModel.LastModifiedFromPlatform),
				nameof(KitchenProductionReturnItemOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenProductionReturnItemOverviewModel.MasterStatus));
		}

		else
		{
			columnOrder =
			[
				nameof(KitchenProductionReturnItemOverviewModel.ItemName),
				nameof(KitchenProductionReturnItemOverviewModel.ItemCode),
				nameof(KitchenProductionReturnItemOverviewModel.TransactionNo),
				nameof(KitchenProductionReturnItemOverviewModel.TransactionDateTime),
				nameof(KitchenProductionReturnItemOverviewModel.KitchenName),
				nameof(KitchenProductionReturnItemOverviewModel.Quantity),
				nameof(KitchenProductionReturnItemOverviewModel.Rate),
				nameof(KitchenProductionReturnItemOverviewModel.Total)
			];

			if (product is not null)
				columnOrder.Remove(nameof(KitchenProductionReturnItemOverviewModel.ItemName));

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenProductionReturnItemOverviewModel.KitchenName));
		}

		string fileName = $"KITCHEN_PRODUCTION_RETURN_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				kitchenProductionReturnItemData,
				"KITCHEN PRODUCTION RETURN ITEM REPORT",
				currentDateTime,
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
			var stream = ExcelReportExportUtil.ExportToExcel(
				kitchenProductionReturnItemData,
				"KITCHEN PRODUCTION RETURN ITEM REPORT",
				"Kitchen Production Return Item Transactions",
				currentDateTime,
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

using PrimeBakes.Library.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Library.Inventory.Kitchen.Exports;

public static class KitchenIssueReturnReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<KitchenIssueReturnOverviewModel> kitchenIssueReturnData,
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
			[nameof(KitchenIssueReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueReturnOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(KitchenIssueReturnOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenIssueReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
		{
			columnOrder =
			[
				nameof(KitchenIssueReturnOverviewModel.KitchenName),
				nameof(KitchenIssueReturnOverviewModel.TotalItems),
				nameof(KitchenIssueReturnOverviewModel.TotalQuantity),
				nameof(KitchenIssueReturnOverviewModel.TotalAmount)
			];

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenIssueReturnOverviewModel.KitchenName));
		}

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(KitchenIssueReturnOverviewModel.TransactionNo),
				nameof(KitchenIssueReturnOverviewModel.CompanyName),
				nameof(KitchenIssueReturnOverviewModel.TransactionDateTime),
				nameof(KitchenIssueReturnOverviewModel.FinancialYear),
				nameof(KitchenIssueReturnOverviewModel.KitchenName),
				nameof(KitchenIssueReturnOverviewModel.TotalItems),
				nameof(KitchenIssueReturnOverviewModel.TotalQuantity),
				nameof(KitchenIssueReturnOverviewModel.TotalAmount),
				nameof(KitchenIssueReturnOverviewModel.Remarks),
				nameof(KitchenIssueReturnOverviewModel.CreatedByName),
				nameof(KitchenIssueReturnOverviewModel.CreatedAt),
				nameof(KitchenIssueReturnOverviewModel.CreatedFromPlatform),
				nameof(KitchenIssueReturnOverviewModel.LastModifiedByUserName),
				nameof(KitchenIssueReturnOverviewModel.LastModifiedAt),
				nameof(KitchenIssueReturnOverviewModel.LastModifiedFromPlatform),
				nameof(KitchenIssueReturnOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenIssueReturnOverviewModel.Status));
		}

		else
		{
			columnOrder =
			[
				nameof(KitchenIssueReturnOverviewModel.TransactionNo),
				nameof(KitchenIssueReturnOverviewModel.TransactionDateTime),
				nameof(KitchenIssueReturnOverviewModel.KitchenName),
				nameof(KitchenIssueReturnOverviewModel.TotalQuantity),
				nameof(KitchenIssueReturnOverviewModel.TotalAmount),
				nameof(KitchenIssueReturnOverviewModel.Status)
			];

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenIssueReturnOverviewModel.KitchenName));

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenIssueReturnOverviewModel.Status));
		}

		string fileName = $"KITCHEN_ISSUE_RETURN_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				kitchenIssueReturnData,
				"KITCHEN ISSUE RETURN REPORT",
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
				kitchenIssueReturnData,
				"KITCHEN ISSUE RETURN REPORT",
				"Kitchen Issue Return Transactions",
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
		IEnumerable<KitchenIssueReturnItemOverviewModel> kitchenIssueReturnItemData,
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
			[nameof(KitchenIssueReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Raw Material", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenIssueReturnItemOverviewModel.UnitOfMeasurement)] = new() { DisplayName = "UOM", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(KitchenIssueReturnItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(KitchenIssueReturnItemOverviewModel.ItemRemarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.KitchenName)] = new() { DisplayName = "Kitchen", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.KitchenIssueReturnRemarks)] = new() { DisplayName = "Return Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.TotalItems)] = new() { DisplayName = "Total Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.TotalQuantity)] = new() { DisplayName = "Total Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.TotalAmount)] = new() { DisplayName = "Total Amount", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(KitchenIssueReturnItemOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(KitchenIssueReturnItemOverviewModel.ItemName),
				nameof(KitchenIssueReturnItemOverviewModel.ItemCode),
				nameof(KitchenIssueReturnItemOverviewModel.ItemCategoryName),
				nameof(KitchenIssueReturnItemOverviewModel.Quantity),
				nameof(KitchenIssueReturnItemOverviewModel.Total)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(KitchenIssueReturnItemOverviewModel.ItemName),
				nameof(KitchenIssueReturnItemOverviewModel.ItemCode),
				nameof(KitchenIssueReturnItemOverviewModel.ItemCategoryName),
				nameof(KitchenIssueReturnItemOverviewModel.Quantity),
				nameof(KitchenIssueReturnItemOverviewModel.UnitOfMeasurement),
				nameof(KitchenIssueReturnItemOverviewModel.Rate),
				nameof(KitchenIssueReturnItemOverviewModel.Total),
				nameof(KitchenIssueReturnItemOverviewModel.ItemRemarks),
				nameof(KitchenIssueReturnItemOverviewModel.TransactionNo),
				nameof(KitchenIssueReturnItemOverviewModel.CompanyName),
				nameof(KitchenIssueReturnItemOverviewModel.TransactionDateTime),
				nameof(KitchenIssueReturnItemOverviewModel.FinancialYear),
				nameof(KitchenIssueReturnItemOverviewModel.KitchenName),
				nameof(KitchenIssueReturnItemOverviewModel.KitchenIssueReturnRemarks),
				nameof(KitchenIssueReturnItemOverviewModel.TotalItems),
				nameof(KitchenIssueReturnItemOverviewModel.TotalQuantity),
				nameof(KitchenIssueReturnItemOverviewModel.TotalAmount),
				nameof(KitchenIssueReturnItemOverviewModel.CreatedByName),
				nameof(KitchenIssueReturnItemOverviewModel.CreatedAt),
				nameof(KitchenIssueReturnItemOverviewModel.CreatedFromPlatform),
				nameof(KitchenIssueReturnItemOverviewModel.LastModifiedByUserName),
				nameof(KitchenIssueReturnItemOverviewModel.LastModifiedAt),
				nameof(KitchenIssueReturnItemOverviewModel.LastModifiedFromPlatform),
				nameof(KitchenIssueReturnItemOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(KitchenIssueReturnItemOverviewModel.MasterStatus));
		}

		else
		{
			columnOrder =
			[
				nameof(KitchenIssueReturnItemOverviewModel.ItemName),
				nameof(KitchenIssueReturnItemOverviewModel.ItemCode),
				nameof(KitchenIssueReturnItemOverviewModel.Quantity),
				nameof(KitchenIssueReturnItemOverviewModel.Rate),
				nameof(KitchenIssueReturnItemOverviewModel.Total),
				nameof(KitchenIssueReturnItemOverviewModel.TransactionNo),
				nameof(KitchenIssueReturnItemOverviewModel.TransactionDateTime),
				nameof(KitchenIssueReturnItemOverviewModel.KitchenName)
			];

			if (rawMaterial is not null)
				columnOrder.Remove(nameof(KitchenIssueReturnItemOverviewModel.ItemName));

			if (kitchen is not null)
				columnOrder.Remove(nameof(KitchenIssueReturnItemOverviewModel.KitchenName));
		}

		string fileName = $"KITCHEN_ISSUE_RETURN_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				kitchenIssueReturnItemData,
				"KITCHEN ISSUE RETURN ITEM REPORT",
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
				kitchenIssueReturnItemData,
				"KITCHEN ISSUE RETURN ITEM REPORT",
				"Kitchen Issue Return Item Transactions",
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

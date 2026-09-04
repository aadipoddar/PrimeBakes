using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Exports.Inventory.Kitchen.KitchenIssue;

public static class KitchenIssueReturnReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<KitchenIssueReturnOverviewModel> kitchenIssueReturnData,
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
			[nameof(KitchenIssueReturnOverviewModel.CreatedFormFactor)] = new() { DisplayName = "Created Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.CreatedPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.CreatedLatitude)] = new() { DisplayName = "Created Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.CreatedLongitude)] = new() { DisplayName = "Created Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.LastModifiedFormFactor)] = new() { DisplayName = "Modified Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.LastModifiedPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.LastModifiedLatitude)] = new() { DisplayName = "Modified Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.LastModifiedLongitude)] = new() { DisplayName = "Modified Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.CreatedUserOffset)] = new() { DisplayName = "Created Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnOverviewModel.LastModifiedUserOffset)] = new() { DisplayName = "Modified Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
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
				nameof(KitchenIssueReturnOverviewModel.CreatedFormFactor),
				nameof(KitchenIssueReturnOverviewModel.CreatedPlatform),
				nameof(KitchenIssueReturnOverviewModel.CreatedLatitude),
				nameof(KitchenIssueReturnOverviewModel.CreatedLongitude),
				nameof(KitchenIssueReturnOverviewModel.LastModifiedByUserName),
				nameof(KitchenIssueReturnOverviewModel.LastModifiedAt),
				nameof(KitchenIssueReturnOverviewModel.LastModifiedFormFactor),
				nameof(KitchenIssueReturnOverviewModel.LastModifiedPlatform),
				nameof(KitchenIssueReturnOverviewModel.LastModifiedLatitude),
				nameof(KitchenIssueReturnOverviewModel.LastModifiedLongitude),
				nameof(KitchenIssueReturnOverviewModel.CreatedUserOffset),
				nameof(KitchenIssueReturnOverviewModel.LastModifiedUserOffset),
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
			var stream = PDFReportExportUtil.ExportToPdf(
				kitchenIssueReturnData,
				"KITCHEN ISSUE RETURN REPORT",
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
				kitchenIssueReturnData,
				"KITCHEN ISSUE RETURN REPORT",
				"Kitchen Issue Return Transactions",
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
		IEnumerable<KitchenIssueReturnItemOverviewModel> kitchenIssueReturnItemData,
		DateTime currentDateTime,
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
			[nameof(KitchenIssueReturnItemOverviewModel.CreatedFormFactor)] = new() { DisplayName = "Created Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.CreatedPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.CreatedLatitude)] = new() { DisplayName = "Created Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.CreatedLongitude)] = new() { DisplayName = "Created Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.LastModifiedFormFactor)] = new() { DisplayName = "Modified Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.LastModifiedPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.LastModifiedLatitude)] = new() { DisplayName = "Modified Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.LastModifiedLongitude)] = new() { DisplayName = "Modified Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.CreatedUserOffset)] = new() { DisplayName = "Created Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(KitchenIssueReturnItemOverviewModel.LastModifiedUserOffset)] = new() { DisplayName = "Modified Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
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
				nameof(KitchenIssueReturnItemOverviewModel.CreatedFormFactor),
				nameof(KitchenIssueReturnItemOverviewModel.CreatedPlatform),
				nameof(KitchenIssueReturnItemOverviewModel.CreatedLatitude),
				nameof(KitchenIssueReturnItemOverviewModel.CreatedLongitude),
				nameof(KitchenIssueReturnItemOverviewModel.LastModifiedByUserName),
				nameof(KitchenIssueReturnItemOverviewModel.LastModifiedAt),
				nameof(KitchenIssueReturnItemOverviewModel.LastModifiedFormFactor),
				nameof(KitchenIssueReturnItemOverviewModel.LastModifiedPlatform),
				nameof(KitchenIssueReturnItemOverviewModel.LastModifiedLatitude),
				nameof(KitchenIssueReturnItemOverviewModel.LastModifiedLongitude),
				nameof(KitchenIssueReturnItemOverviewModel.CreatedUserOffset),
				nameof(KitchenIssueReturnItemOverviewModel.LastModifiedUserOffset),
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
			var stream = PDFReportExportUtil.ExportToPdf(
				kitchenIssueReturnItemData,
				"KITCHEN ISSUE RETURN ITEM REPORT",
				currentDateTime,
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
			var stream = ExcelReportExportUtil.ExportToExcel(
				kitchenIssueReturnItemData,
				"KITCHEN ISSUE RETURN ITEM REPORT",
				"Kitchen Issue Return Item Transactions",
				currentDateTime,
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

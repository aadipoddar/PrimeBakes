using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.PurchaseOrder;
using PrimeBakes.Models.Inventory.RawMaterial;

namespace PrimeBakes.Exports.Inventory.PurchaseOrder;

public static class PurchaseOrderReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<PurchaseOrderOverviewModel> purchaseOrderData,
		DateTime currentDateTime,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		LedgerModel party = null,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(PurchaseOrderOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.PurchaseTransactionNo)] = new() { DisplayName = "Purchase Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.ExpectedDeliveryDate)] = new() { DisplayName = "Expected Delivery", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.PurchaseDateTime)] = new() { DisplayName = "Purchase Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.CreatedFormFactor)] = new() { DisplayName = "Created Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.CreatedPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.CreatedLatitude)] = new() { DisplayName = "Created Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.CreatedLongitude)] = new() { DisplayName = "Created Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.LastModifiedFormFactor)] = new() { DisplayName = "Modified Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.LastModifiedPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.LastModifiedLatitude)] = new() { DisplayName = "Modified Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.LastModifiedLongitude)] = new() { DisplayName = "Modified Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.CreatedUserOffset)] = new() { DisplayName = "Created Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.LastModifiedUserOffset)] = new() { DisplayName = "Modified Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(PurchaseOrderOverviewModel.PartyName),
				nameof(PurchaseOrderOverviewModel.TotalItems),
				nameof(PurchaseOrderOverviewModel.TotalQuantity)
			];
		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(PurchaseOrderOverviewModel.TransactionNo),
				nameof(PurchaseOrderOverviewModel.PurchaseTransactionNo),
				nameof(PurchaseOrderOverviewModel.CompanyName),
				nameof(PurchaseOrderOverviewModel.PartyName),
				nameof(PurchaseOrderOverviewModel.TransactionDateTime),
				nameof(PurchaseOrderOverviewModel.ExpectedDeliveryDate),
				nameof(PurchaseOrderOverviewModel.PurchaseDateTime),
				nameof(PurchaseOrderOverviewModel.FinancialYear),
				nameof(PurchaseOrderOverviewModel.TotalItems),
				nameof(PurchaseOrderOverviewModel.TotalQuantity),
				nameof(PurchaseOrderOverviewModel.Remarks),
				nameof(PurchaseOrderOverviewModel.CreatedByName),
				nameof(PurchaseOrderOverviewModel.CreatedAt),
				nameof(PurchaseOrderOverviewModel.CreatedFormFactor),
				nameof(PurchaseOrderOverviewModel.CreatedPlatform),
				nameof(PurchaseOrderOverviewModel.CreatedLatitude),
				nameof(PurchaseOrderOverviewModel.CreatedLongitude),
				nameof(PurchaseOrderOverviewModel.LastModifiedByUserName),
				nameof(PurchaseOrderOverviewModel.LastModifiedAt),
				nameof(PurchaseOrderOverviewModel.LastModifiedFormFactor),
				nameof(PurchaseOrderOverviewModel.LastModifiedPlatform),
				nameof(PurchaseOrderOverviewModel.LastModifiedLatitude),
				nameof(PurchaseOrderOverviewModel.LastModifiedLongitude),
				nameof(PurchaseOrderOverviewModel.CreatedUserOffset),
				nameof(PurchaseOrderOverviewModel.LastModifiedUserOffset),
				nameof(PurchaseOrderOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(PurchaseOrderOverviewModel.Status));
		}
		else
		{
			columnOrder =
			[
				nameof(PurchaseOrderOverviewModel.TransactionNo),
				nameof(PurchaseOrderOverviewModel.PurchaseTransactionNo),
				nameof(PurchaseOrderOverviewModel.PartyName),
				nameof(PurchaseOrderOverviewModel.TransactionDateTime),
				nameof(PurchaseOrderOverviewModel.ExpectedDeliveryDate),
				nameof(PurchaseOrderOverviewModel.TotalItems),
				nameof(PurchaseOrderOverviewModel.TotalQuantity),
				nameof(PurchaseOrderOverviewModel.Status)
			];

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseOrderOverviewModel.PartyName));

			if (!showDeleted)
				columnOrder.Remove(nameof(PurchaseOrderOverviewModel.Status));
		}

		string fileName = "PURCHASE_ORDER_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				purchaseOrderData,
				"PURCHASE ORDER REPORT",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns && !showSummary,
				new() { ["Company"] = company?.Name ?? null, ["Party"] = party?.Name ?? null }
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = ExcelReportExportUtil.ExportToExcel(
				purchaseOrderData,
				"PURCHASE ORDER REPORT",
				"Purchase Order Transactions",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new()
				{
					["Company"] = company?.Name ?? null,
					["Party"] = party?.Name ?? null
				}
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}

	public static (MemoryStream stream, string fileName) ExportItemReport(
		IEnumerable<PurchaseOrderItemOverviewModel> purchaseOrderItemData,
		DateTime currentDateTime,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		RawMaterialModel rawMaterial = null,
		RawMaterialCategoryModel rawMaterialCategory = null,
		CompanyModel company = null,
		LedgerModel party = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(PurchaseOrderItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.ItemRemarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseOrderItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseOrderItemOverviewModel.UnitOfMeasurement)] = new() { DisplayName = "Unit", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(PurchaseOrderItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.PurchaseTransactionNo)] = new() { DisplayName = "Purchase Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.ExpectedDeliveryDate)] = new() { DisplayName = "Expected Delivery", Format = "dd-MMM-yyyy", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.PurchaseDateTime)] = new() { DisplayName = "Purchase Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseOrderItemOverviewModel.TotalItems)] = new() { DisplayName = "Order Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.TotalQuantity)] = new() { DisplayName = "Order Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseOrderItemOverviewModel.Remarks)] = new() { DisplayName = "Order Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.CreatedFormFactor)] = new() { DisplayName = "Created Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.CreatedPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.CreatedLatitude)] = new() { DisplayName = "Created Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.CreatedLongitude)] = new() { DisplayName = "Created Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.LastModifiedFormFactor)] = new() { DisplayName = "Modified Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.LastModifiedPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.LastModifiedLatitude)] = new() { DisplayName = "Modified Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.LastModifiedLongitude)] = new() { DisplayName = "Modified Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.CreatedUserOffset)] = new() { DisplayName = "Created Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseOrderItemOverviewModel.LastModifiedUserOffset)] = new() { DisplayName = "Modified Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(PurchaseOrderItemOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(PurchaseOrderItemOverviewModel.ItemName),
				nameof(PurchaseOrderItemOverviewModel.ItemCode),
				nameof(PurchaseOrderItemOverviewModel.ItemCategoryName),
				nameof(PurchaseOrderItemOverviewModel.Quantity),
				nameof(PurchaseOrderItemOverviewModel.UnitOfMeasurement)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(PurchaseOrderItemOverviewModel.ItemName),
				nameof(PurchaseOrderItemOverviewModel.ItemCode),
				nameof(PurchaseOrderItemOverviewModel.ItemCategoryName),
				nameof(PurchaseOrderItemOverviewModel.TransactionNo),
				nameof(PurchaseOrderItemOverviewModel.PurchaseTransactionNo),
				nameof(PurchaseOrderItemOverviewModel.TransactionDateTime),
				nameof(PurchaseOrderItemOverviewModel.ExpectedDeliveryDate),
				nameof(PurchaseOrderItemOverviewModel.PurchaseDateTime),
				nameof(PurchaseOrderItemOverviewModel.CompanyName),
				nameof(PurchaseOrderItemOverviewModel.PartyName),
				nameof(PurchaseOrderItemOverviewModel.FinancialYear),
				nameof(PurchaseOrderItemOverviewModel.Quantity),
				nameof(PurchaseOrderItemOverviewModel.UnitOfMeasurement),
				nameof(PurchaseOrderItemOverviewModel.ItemRemarks),
				nameof(PurchaseOrderItemOverviewModel.Remarks),
				nameof(PurchaseOrderItemOverviewModel.TotalItems),
				nameof(PurchaseOrderItemOverviewModel.TotalQuantity),
				nameof(PurchaseOrderItemOverviewModel.CreatedByName),
				nameof(PurchaseOrderItemOverviewModel.CreatedAt),
				nameof(PurchaseOrderItemOverviewModel.CreatedFormFactor),
				nameof(PurchaseOrderItemOverviewModel.CreatedPlatform),
				nameof(PurchaseOrderItemOverviewModel.CreatedLatitude),
				nameof(PurchaseOrderItemOverviewModel.CreatedLongitude),
				nameof(PurchaseOrderItemOverviewModel.LastModifiedByUserName),
				nameof(PurchaseOrderItemOverviewModel.LastModifiedAt),
				nameof(PurchaseOrderItemOverviewModel.LastModifiedFormFactor),
				nameof(PurchaseOrderItemOverviewModel.LastModifiedPlatform),
				nameof(PurchaseOrderItemOverviewModel.LastModifiedLatitude),
				nameof(PurchaseOrderItemOverviewModel.LastModifiedLongitude),
				nameof(PurchaseOrderItemOverviewModel.CreatedUserOffset),
				nameof(PurchaseOrderItemOverviewModel.LastModifiedUserOffset),
				nameof(PurchaseOrderItemOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(PurchaseOrderItemOverviewModel.MasterStatus));
		}

		else
		{
			columnOrder =
			[
				nameof(PurchaseOrderItemOverviewModel.ItemName),
				nameof(PurchaseOrderItemOverviewModel.ItemCode),
				nameof(PurchaseOrderItemOverviewModel.TransactionNo),
				nameof(PurchaseOrderItemOverviewModel.TransactionDateTime),
				nameof(PurchaseOrderItemOverviewModel.PartyName),
				nameof(PurchaseOrderItemOverviewModel.PurchaseTransactionNo),
				nameof(PurchaseOrderItemOverviewModel.Quantity),
				nameof(PurchaseOrderItemOverviewModel.UnitOfMeasurement),
				nameof(PurchaseOrderItemOverviewModel.MasterStatus)
			];

			if (rawMaterial is not null)
				columnOrder.Remove(nameof(PurchaseOrderItemOverviewModel.ItemName));

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseOrderItemOverviewModel.PartyName));

			if (!showDeleted)
				columnOrder.Remove(nameof(PurchaseOrderItemOverviewModel.MasterStatus));
		}

		string fileName = "PURCHASE_ORDER_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				purchaseOrderItemData,
				"PURCHASE ORDER ITEM REPORT",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new()
				{
					["Item"] = rawMaterial?.Name ?? null,
					["Item Category"] = rawMaterialCategory?.Name ?? null,
					["Company"] = company?.Name ?? null,
					["Party"] = party?.Name ?? null
				}
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = ExcelReportExportUtil.ExportToExcel(
				purchaseOrderItemData,
				"PURCHASE ORDER ITEM REPORT",
				"Purchase Order Item Transactions",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new()
				{
					["Item"] = rawMaterial?.Name ?? null,
					["Item Category"] = rawMaterialCategory?.Name ?? null,
					["Company"] = company?.Name ?? null,
					["Party"] = party?.Name ?? null
				}
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

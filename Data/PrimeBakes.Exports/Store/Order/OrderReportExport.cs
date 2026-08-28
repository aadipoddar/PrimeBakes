using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Store.Order;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Exports.Store.Order;

public static class OrderReportExport
{
	public static (MemoryStream stream, string fileName) ExportReport(
		IEnumerable<OrderOverviewModel> orderData,
		DateTime currentDateTime,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		CompanyModel company = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(OrderOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.SaleTransactionNo)] = new() { DisplayName = "Sale Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OrderOverviewModel.SaleDateTime)] = new() { DisplayName = "Sale Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OrderOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OrderOverviewModel.CreatedFormFactor)] = new() { DisplayName = "Created Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.CreatedPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.CreatedLatitude)] = new() { DisplayName = "Created Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderOverviewModel.CreatedLongitude)] = new() { DisplayName = "Created Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OrderOverviewModel.LastModifiedFormFactor)] = new() { DisplayName = "Modified Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.LastModifiedPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderOverviewModel.LastModifiedLatitude)] = new() { DisplayName = "Modified Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderOverviewModel.LastModifiedLongitude)] = new() { DisplayName = "Modified Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderOverviewModel.CreatedLocationOffset)] = new() { DisplayName = "Created Offset (Location)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderOverviewModel.CreatedUserOffset)] = new() { DisplayName = "Created Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderOverviewModel.LastModifiedLocationOffset)] = new() { DisplayName = "Modified Offset (Location)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderOverviewModel.LastModifiedUserOffset)] = new() { DisplayName = "Modified Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(OrderOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(OrderOverviewModel.LocationName),
				nameof(OrderOverviewModel.TotalItems),
				nameof(OrderOverviewModel.TotalQuantity)
			];
		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(OrderOverviewModel.TransactionNo),
				nameof(OrderOverviewModel.SaleTransactionNo),
				nameof(OrderOverviewModel.CompanyName),
				nameof(OrderOverviewModel.LocationName),
				nameof(OrderOverviewModel.TransactionDateTime),
				nameof(OrderOverviewModel.SaleDateTime),
				nameof(OrderOverviewModel.FinancialYear),
				nameof(OrderOverviewModel.TotalItems),
				nameof(OrderOverviewModel.TotalQuantity),
				nameof(OrderOverviewModel.Remarks),
				nameof(OrderOverviewModel.CreatedByName),
				nameof(OrderOverviewModel.CreatedAt),
				nameof(OrderOverviewModel.CreatedFormFactor),
				nameof(OrderOverviewModel.CreatedPlatform),
				nameof(OrderOverviewModel.CreatedLatitude),
				nameof(OrderOverviewModel.CreatedLongitude),
				nameof(OrderOverviewModel.LastModifiedByUserName),
				nameof(OrderOverviewModel.LastModifiedAt),
				nameof(OrderOverviewModel.LastModifiedFormFactor),
				nameof(OrderOverviewModel.LastModifiedPlatform),
				nameof(OrderOverviewModel.LastModifiedLatitude),
				nameof(OrderOverviewModel.LastModifiedLongitude),
				nameof(OrderOverviewModel.CreatedLocationOffset),
				nameof(OrderOverviewModel.CreatedUserOffset),
				nameof(OrderOverviewModel.LastModifiedLocationOffset),
				nameof(OrderOverviewModel.LastModifiedUserOffset),
				nameof(OrderOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(OrderOverviewModel.Status));
		}
		else
		{
			columnOrder =
			[
				nameof(OrderOverviewModel.TransactionNo),
				nameof(OrderOverviewModel.SaleTransactionNo),
				nameof(OrderOverviewModel.LocationName),
				nameof(OrderOverviewModel.TransactionDateTime),
				nameof(OrderOverviewModel.TotalItems),
				nameof(OrderOverviewModel.TotalQuantity),
				nameof(OrderOverviewModel.Status)
			];

			if (location is not null)
				columnOrder.Remove(nameof(OrderOverviewModel.LocationName));

			if (!showDeleted)
				columnOrder.Remove(nameof(OrderOverviewModel.Status));
		}

		string fileName = "ORDER_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				orderData,
				"ORDER REPORT",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns && !showSummary,
				new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null }
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = ExcelReportExportUtil.ExportToExcel(
				orderData,
				"ORDER REPORT",
				"Order Transactions",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new()
				{
					["Company"] = company?.Name ?? null,
					["Location"] = location?.Name ?? null
				}
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}

	public static (MemoryStream stream, string fileName) ExportItemReport(
		IEnumerable<OrderItemOverviewModel> orderItemData,
		DateTime currentDateTime,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		ProductModel product = null,
		ProductCategoryModel productCategory = null,
		CompanyModel company = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(OrderItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.ItemRemarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(OrderItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },

			[nameof(OrderItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.SaleTransactionNo)] = new() { DisplayName = "Sale Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.SaleDateTime)] = new() { DisplayName = "Sale Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(OrderItemOverviewModel.TotalItems)] = new() { DisplayName = "Order Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.TotalQuantity)] = new() { DisplayName = "Order Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(OrderItemOverviewModel.Remarks)] = new() { DisplayName = "Order Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.CreatedFormFactor)] = new() { DisplayName = "Created Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.CreatedPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.CreatedLatitude)] = new() { DisplayName = "Created Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.CreatedLongitude)] = new() { DisplayName = "Created Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.LastModifiedFormFactor)] = new() { DisplayName = "Modified Form", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.LastModifiedPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.LastModifiedLatitude)] = new() { DisplayName = "Modified Lat", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.LastModifiedLongitude)] = new() { DisplayName = "Modified Long", Format = "0.000000", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.CreatedLocationOffset)] = new() { DisplayName = "Created Offset (Location)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.CreatedUserOffset)] = new() { DisplayName = "Created Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.LastModifiedLocationOffset)] = new() { DisplayName = "Modified Offset (Location)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(OrderItemOverviewModel.LastModifiedUserOffset)] = new() { DisplayName = "Modified Offset (User)", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },

			[nameof(OrderItemOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false }
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(OrderItemOverviewModel.ItemName),
				nameof(OrderItemOverviewModel.ItemCode),
				nameof(OrderItemOverviewModel.ItemCategoryName),
				nameof(OrderItemOverviewModel.Quantity)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(OrderItemOverviewModel.ItemName),
				nameof(OrderItemOverviewModel.ItemCode),
				nameof(OrderItemOverviewModel.ItemCategoryName),
				nameof(OrderItemOverviewModel.TransactionNo),
				nameof(OrderItemOverviewModel.SaleTransactionNo),
				nameof(OrderItemOverviewModel.TransactionDateTime),
				nameof(OrderItemOverviewModel.SaleDateTime),
				nameof(OrderItemOverviewModel.CompanyName),
				nameof(OrderItemOverviewModel.LocationName),
				nameof(OrderItemOverviewModel.FinancialYear),
				nameof(OrderItemOverviewModel.Quantity),
				nameof(OrderItemOverviewModel.ItemRemarks),
				nameof(OrderItemOverviewModel.Remarks),
				nameof(OrderItemOverviewModel.TotalItems),
				nameof(OrderItemOverviewModel.TotalQuantity),
				nameof(OrderItemOverviewModel.CreatedByName),
				nameof(OrderItemOverviewModel.CreatedAt),
				nameof(OrderItemOverviewModel.CreatedFormFactor),
				nameof(OrderItemOverviewModel.CreatedPlatform),
				nameof(OrderItemOverviewModel.CreatedLatitude),
				nameof(OrderItemOverviewModel.CreatedLongitude),
				nameof(OrderItemOverviewModel.LastModifiedByUserName),
				nameof(OrderItemOverviewModel.LastModifiedAt),
				nameof(OrderItemOverviewModel.LastModifiedFormFactor),
				nameof(OrderItemOverviewModel.LastModifiedPlatform),
				nameof(OrderItemOverviewModel.LastModifiedLatitude),
				nameof(OrderItemOverviewModel.LastModifiedLongitude),
				nameof(OrderItemOverviewModel.CreatedLocationOffset),
				nameof(OrderItemOverviewModel.CreatedUserOffset),
				nameof(OrderItemOverviewModel.LastModifiedLocationOffset),
				nameof(OrderItemOverviewModel.LastModifiedUserOffset),
				nameof(OrderItemOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(OrderItemOverviewModel.MasterStatus));
		}

		else
		{
			columnOrder =
			[
				nameof(OrderItemOverviewModel.ItemName),
				nameof(OrderItemOverviewModel.ItemCode),
				nameof(OrderItemOverviewModel.TransactionNo),
				nameof(OrderItemOverviewModel.TransactionDateTime),
				nameof(OrderItemOverviewModel.LocationName),
				nameof(OrderItemOverviewModel.SaleTransactionNo),
				nameof(OrderItemOverviewModel.Quantity),
				nameof(OrderItemOverviewModel.MasterStatus)
			];

			if (product is not null)
				columnOrder.Remove(nameof(OrderItemOverviewModel.ItemName));

			if (location is not null)
				columnOrder.Remove(nameof(OrderItemOverviewModel.LocationName));

			if (!showDeleted)
				columnOrder.Remove(nameof(OrderItemOverviewModel.MasterStatus));
		}

		string fileName = "ORDER_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = PDFReportExportUtil.ExportToPdf(
				orderItemData,
				"ORDER ITEM REPORT",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new()
				{
					["Item"] = product?.Name ?? null,
					["Item Category"] = productCategory?.Name ?? null,
					["Company"] = company?.Name ?? null,
					["Location"] = location?.Name ?? null
				}
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = ExcelReportExportUtil.ExportToExcel(
				orderItemData,
				"ORDER ITEM REPORT",
				"Order Item Transactions",
				currentDateTime,
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new()
				{
					["Item"] = product?.Name ?? null,
					["Item Category"] = productCategory?.Name ?? null,
					["Company"] = company?.Name ?? null,
					["Location"] = location?.Name ?? null
				}
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

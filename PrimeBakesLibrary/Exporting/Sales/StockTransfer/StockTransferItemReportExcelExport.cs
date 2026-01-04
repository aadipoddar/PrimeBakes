using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Exporting.Sales.StockTransfer;

public static class StockTransferItemReportExcelExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<StockTransferItemOverviewModel> stockTransferItemData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		CompanyModel company = null,
		LocationModel fromLocation = null,
		LocationModel toLocation = null)
	{
		var columnSettings = new Dictionary<string, ExcelReportExportUtil.ColumnSetting>
		{
			[nameof(StockTransferItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.LocationName)] = new() { DisplayName = "From Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.ToLocationName)] = new() { DisplayName = "To Location", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.StockTransferRemarks)] = new() { DisplayName = "Stock Transfer Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignLeft, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Numeric fields - Quantity
			[nameof(StockTransferItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = false },

			// Amount fields - All with N2 format and totals
			[nameof(StockTransferItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Discount Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },
			[nameof(StockTransferItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignRight, IncludeInTotal = true },

			// Percentage fields - Center aligned
			[nameof(StockTransferItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false },

			// Boolean fields
			[nameof(StockTransferItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = Syncfusion.XlsIO.ExcelHAlign.HAlignCenter, IncludeInTotal = false }
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(StockTransferItemOverviewModel.ItemName),
				nameof(StockTransferItemOverviewModel.ItemCode),
				nameof(StockTransferItemOverviewModel.ItemCategoryName),
				nameof(StockTransferItemOverviewModel.Quantity),
				nameof(StockTransferItemOverviewModel.BaseTotal),
				nameof(StockTransferItemOverviewModel.DiscountAmount),
				nameof(StockTransferItemOverviewModel.AfterDiscount),
				nameof(StockTransferItemOverviewModel.SGSTAmount),
				nameof(StockTransferItemOverviewModel.CGSTAmount),
				nameof(StockTransferItemOverviewModel.IGSTAmount),
				nameof(StockTransferItemOverviewModel.TotalTaxAmount),
				nameof(StockTransferItemOverviewModel.Total),
				nameof(StockTransferItemOverviewModel.NetTotal)
			];
		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(StockTransferItemOverviewModel.ItemName),
				nameof(StockTransferItemOverviewModel.ItemCode),
				nameof(StockTransferItemOverviewModel.ItemCategoryName),
				nameof(StockTransferItemOverviewModel.TransactionNo),
				nameof(StockTransferItemOverviewModel.TransactionDateTime),
				nameof(StockTransferItemOverviewModel.CompanyName),
				nameof(StockTransferItemOverviewModel.LocationName),
				nameof(StockTransferItemOverviewModel.ToLocationName),
				nameof(StockTransferItemOverviewModel.Quantity),
				nameof(StockTransferItemOverviewModel.Rate),
				nameof(StockTransferItemOverviewModel.BaseTotal),
				nameof(StockTransferItemOverviewModel.DiscountPercent),
				nameof(StockTransferItemOverviewModel.DiscountAmount),
				nameof(StockTransferItemOverviewModel.AfterDiscount),
				nameof(StockTransferItemOverviewModel.SGSTPercent),
				nameof(StockTransferItemOverviewModel.SGSTAmount),
				nameof(StockTransferItemOverviewModel.CGSTPercent),
				nameof(StockTransferItemOverviewModel.CGSTAmount),
				nameof(StockTransferItemOverviewModel.IGSTPercent),
				nameof(StockTransferItemOverviewModel.IGSTAmount),
				nameof(StockTransferItemOverviewModel.TotalTaxAmount),
				nameof(StockTransferItemOverviewModel.InclusiveTax),
				nameof(StockTransferItemOverviewModel.Total),
				nameof(StockTransferItemOverviewModel.NetRate),
				nameof(StockTransferItemOverviewModel.NetTotal),
				nameof(StockTransferItemOverviewModel.StockTransferRemarks),
				nameof(StockTransferItemOverviewModel.Remarks)
			];
		}
		else
		{
			columnOrder =
			[
				nameof(StockTransferItemOverviewModel.ItemName),
				nameof(StockTransferItemOverviewModel.ItemCode),
				nameof(StockTransferItemOverviewModel.TransactionNo),
				nameof(StockTransferItemOverviewModel.TransactionDateTime),
				nameof(StockTransferItemOverviewModel.LocationName),
				nameof(StockTransferItemOverviewModel.ToLocationName),
				nameof(StockTransferItemOverviewModel.Quantity),
				nameof(StockTransferItemOverviewModel.NetRate),
				nameof(StockTransferItemOverviewModel.NetTotal)
			];
		}

		if (company is not null)
			columnOrder.Remove(nameof(StockTransferItemOverviewModel.CompanyName));

		if (fromLocation is not null)
			columnOrder.Remove(nameof(StockTransferItemOverviewModel.LocationName));

		if (toLocation is not null)
			columnOrder.Remove(nameof(StockTransferItemOverviewModel.ToLocationName));

		var stream = await ExcelReportExportUtil.ExportToExcel(
			stockTransferItemData,
			"STOCK TRANSFER ITEM REPORT",
			"Stock Transfer Item Transactions",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			new() { ["Company"] = company?.Name ?? null, ["From Location"] = fromLocation?.Name ?? null, ["To Location"] = toLocation?.Name ?? null }
		);

		string fileName = "STOCK_TRANSFER_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
		fileName += ".xlsx";

		return (stream, fileName);
	}
}

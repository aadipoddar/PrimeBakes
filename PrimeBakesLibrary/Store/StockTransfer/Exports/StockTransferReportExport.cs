using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Store.StockTransfer.Models;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Store.StockTransfer.Exports;

public static class StockTransferReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<StockTransferOverviewModel> data,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		CompanyModel company = null,
		LocationModel fromLocation = null,
		LocationModel toLocation = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(StockTransferOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.LocationName)] = new() { DisplayName = "From Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.ToLocationName)] = new() { DisplayName = "To Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(StockTransferOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(StockTransferOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(StockTransferOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, IsRequired = true, IsGrandTotal = true, HighlightNegative = true },

			[nameof(StockTransferOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(StockTransferOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.FinancialAccountingTransactionNo)] = new() { DisplayName = "Accounting Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(StockTransferOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(StockTransferOverviewModel.ToLocationName),
				nameof(StockTransferOverviewModel.TotalItems),
				nameof(StockTransferOverviewModel.TotalQuantity),
				nameof(StockTransferOverviewModel.BaseTotal),
				nameof(StockTransferOverviewModel.ItemDiscountAmount),
				nameof(StockTransferOverviewModel.TotalAfterItemDiscount),
				nameof(StockTransferOverviewModel.TotalInclusiveTaxAmount),
				nameof(StockTransferOverviewModel.TotalExtraTaxAmount),
				nameof(StockTransferOverviewModel.TotalAfterTax),
				nameof(StockTransferOverviewModel.OtherChargesAmount),
				nameof(StockTransferOverviewModel.DiscountAmount),
				nameof(StockTransferOverviewModel.RoundOffAmount),
				nameof(StockTransferOverviewModel.TotalAmount),
				nameof(StockTransferOverviewModel.Cash),
				nameof(StockTransferOverviewModel.Card),
				nameof(StockTransferOverviewModel.UPI),
				nameof(StockTransferOverviewModel.Credit)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(StockTransferOverviewModel.TransactionNo),
				nameof(StockTransferOverviewModel.CompanyName),
				nameof(StockTransferOverviewModel.LocationName),
				nameof(StockTransferOverviewModel.ToLocationName),
				nameof(StockTransferOverviewModel.TransactionDateTime),
				nameof(StockTransferOverviewModel.FinancialYear),
				nameof(StockTransferOverviewModel.TotalItems),
				nameof(StockTransferOverviewModel.TotalQuantity),
				nameof(StockTransferOverviewModel.BaseTotal),
				nameof(StockTransferOverviewModel.ItemDiscountAmount),
				nameof(StockTransferOverviewModel.TotalAfterItemDiscount),
				nameof(StockTransferOverviewModel.TotalInclusiveTaxAmount),
				nameof(StockTransferOverviewModel.TotalExtraTaxAmount),
				nameof(StockTransferOverviewModel.TotalAfterTax),
				nameof(StockTransferOverviewModel.OtherChargesPercent),
				nameof(StockTransferOverviewModel.OtherChargesAmount),
				nameof(StockTransferOverviewModel.DiscountPercent),
				nameof(StockTransferOverviewModel.DiscountAmount),
				nameof(StockTransferOverviewModel.RoundOffAmount),
				nameof(StockTransferOverviewModel.TotalAmount),
				nameof(StockTransferOverviewModel.Cash),
				nameof(StockTransferOverviewModel.Card),
				nameof(StockTransferOverviewModel.UPI),
				nameof(StockTransferOverviewModel.Credit),
				nameof(StockTransferOverviewModel.PaymentModes),
				nameof(StockTransferOverviewModel.Remarks),
				nameof(StockTransferOverviewModel.FinancialAccountingTransactionNo),
				nameof(StockTransferOverviewModel.CreatedByName),
				nameof(StockTransferOverviewModel.CreatedAt),
				nameof(StockTransferOverviewModel.CreatedFromPlatform),
				nameof(StockTransferOverviewModel.LastModifiedByUserName),
				nameof(StockTransferOverviewModel.LastModifiedAt),
				nameof(StockTransferOverviewModel.LastModifiedFromPlatform),
				nameof(StockTransferOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(StockTransferOverviewModel.Status));
		}

		else
		{
			columnOrder =
			[
				nameof(StockTransferOverviewModel.TransactionNo),
				nameof(StockTransferOverviewModel.LocationName),
				nameof(StockTransferOverviewModel.ToLocationName),
				nameof(StockTransferOverviewModel.TransactionDateTime),
				nameof(StockTransferOverviewModel.TotalQuantity),
				nameof(StockTransferOverviewModel.TotalAfterTax),
				nameof(StockTransferOverviewModel.DiscountPercent),
				nameof(StockTransferOverviewModel.DiscountAmount),
				nameof(StockTransferOverviewModel.TotalAmount),
				nameof(StockTransferOverviewModel.PaymentModes),
				nameof(StockTransferOverviewModel.Status)
			];

			if (fromLocation is not null)
				columnOrder.Remove(nameof(StockTransferOverviewModel.LocationName));

			if (toLocation is not null)
				columnOrder.Remove(nameof(StockTransferOverviewModel.ToLocationName));

			if (!showDeleted)
				columnOrder.Remove(nameof(StockTransferOverviewModel.Status));
		}

		string fileName = "STOCK_TRANSFER_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				data,
				"STOCK TRANSFER REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new() { ["Company"] = company?.Name ?? null, ["From Location"] = fromLocation?.Name ?? null, ["To Location"] = toLocation?.Name ?? null }
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				data,
				"STOCK TRANSFER REPORT",
				"Stock Transfers",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Company"] = company?.Name ?? null, ["From Location"] = fromLocation?.Name ?? null, ["To Location"] = toLocation?.Name ?? null }
			);

			return (stream, fileName + ".xlsx");
		}
	}

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<StockTransferItemOverviewModel> stockTransferItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		ProductModel product = null,
		ProductCategoryModel productCategory = null,
		CompanyModel company = null,
		LocationModel fromLocation = null,
		LocationModel toLocation = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(StockTransferItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.ItemRemarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(StockTransferItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.ItemBaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl. Tax", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(StockTransferItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.LocationName)] = new() { DisplayName = "From Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.ToLocationName)] = new() { DisplayName = "To Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(StockTransferItemOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(StockTransferItemOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.StockTransferDiscountPercent)] = new() { DisplayName = "Bill Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.StockTransferDiscountAmount)] = new() { DisplayName = "Bill Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(StockTransferItemOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(StockTransferItemOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(StockTransferItemOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(StockTransferItemOverviewModel.StockTransferRemarks)] = new() { DisplayName = "Transfer Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.FinancialAccountingTransactionNo)] = new() { DisplayName = "Accounting Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(StockTransferItemOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(StockTransferItemOverviewModel.ItemName),
				nameof(StockTransferItemOverviewModel.ItemCode),
				nameof(StockTransferItemOverviewModel.ItemCategoryName),
				nameof(StockTransferItemOverviewModel.Quantity),
				nameof(StockTransferItemOverviewModel.ItemBaseTotal),
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
				nameof(StockTransferItemOverviewModel.FinancialYear),
				nameof(StockTransferItemOverviewModel.Quantity),
				nameof(StockTransferItemOverviewModel.Rate),
				nameof(StockTransferItemOverviewModel.ItemBaseTotal),
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
				nameof(StockTransferItemOverviewModel.TotalItems),
				nameof(StockTransferItemOverviewModel.TotalQuantity),
				nameof(StockTransferItemOverviewModel.BaseTotal),
				nameof(StockTransferItemOverviewModel.ItemDiscountAmount),
				nameof(StockTransferItemOverviewModel.TotalAfterItemDiscount),
				nameof(StockTransferItemOverviewModel.TotalInclusiveTaxAmount),
				nameof(StockTransferItemOverviewModel.TotalExtraTaxAmount),
				nameof(StockTransferItemOverviewModel.TotalAfterTax),
				nameof(StockTransferItemOverviewModel.OtherChargesPercent),
				nameof(StockTransferItemOverviewModel.OtherChargesAmount),
				nameof(StockTransferItemOverviewModel.StockTransferDiscountPercent),
				nameof(StockTransferItemOverviewModel.StockTransferDiscountAmount),
				nameof(StockTransferItemOverviewModel.RoundOffAmount),
				nameof(StockTransferItemOverviewModel.TotalAmount),
				nameof(StockTransferItemOverviewModel.Cash),
				nameof(StockTransferItemOverviewModel.Card),
				nameof(StockTransferItemOverviewModel.UPI),
				nameof(StockTransferItemOverviewModel.Credit),
				nameof(StockTransferItemOverviewModel.PaymentModes),
				nameof(StockTransferItemOverviewModel.StockTransferRemarks),
				nameof(StockTransferItemOverviewModel.ItemRemarks),
				nameof(StockTransferItemOverviewModel.FinancialAccountingTransactionNo),
				nameof(StockTransferItemOverviewModel.CreatedByName),
				nameof(StockTransferItemOverviewModel.CreatedAt),
				nameof(StockTransferItemOverviewModel.CreatedFromPlatform),
				nameof(StockTransferItemOverviewModel.LastModifiedByUserName),
				nameof(StockTransferItemOverviewModel.LastModifiedAt),
				nameof(StockTransferItemOverviewModel.LastModifiedFromPlatform),
				nameof(StockTransferItemOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(StockTransferItemOverviewModel.MasterStatus));
		}

		else
		{
			columnOrder =
			[
				nameof(StockTransferItemOverviewModel.ItemName),
				nameof(StockTransferItemOverviewModel.Quantity),
				nameof(StockTransferItemOverviewModel.Rate),
				nameof(StockTransferItemOverviewModel.NetRate),
				nameof(StockTransferItemOverviewModel.NetTotal),
				nameof(StockTransferItemOverviewModel.TransactionNo),
				nameof(StockTransferItemOverviewModel.TransactionDateTime),
				nameof(StockTransferItemOverviewModel.LocationName),
				nameof(StockTransferItemOverviewModel.ToLocationName)
			];

			if (product is not null)
				columnOrder.Remove(nameof(StockTransferItemOverviewModel.ItemName));

			if (fromLocation is not null)
				columnOrder.Remove(nameof(StockTransferItemOverviewModel.LocationName));

			if (toLocation is not null)
				columnOrder.Remove(nameof(StockTransferItemOverviewModel.ToLocationName));
		}

		string fileName = "STOCK_TRANSFER_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				stockTransferItemData,
				"STOCK TRANSFER ITEM REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new() { ["Item"] = product?.Name ?? null, ["Category"] = productCategory?.Name ?? null, ["Company"] = company?.Name ?? null, ["From Location"] = fromLocation?.Name ?? null, ["To Location"] = toLocation?.Name ?? null }
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				stockTransferItemData,
				"STOCK TRANSFER ITEM REPORT",
				"Stock Transfer Item Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Item"] = product?.Name ?? null, ["Category"] = productCategory?.Name ?? null, ["Company"] = company?.Name ?? null, ["From Location"] = fromLocation?.Name ?? null, ["To Location"] = toLocation?.Name ?? null }
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

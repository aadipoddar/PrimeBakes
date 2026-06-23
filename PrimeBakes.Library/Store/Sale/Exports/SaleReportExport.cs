using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Store.Product.Models;
using PrimeBakes.Library.Store.Sale.Models;
using PrimeBakes.Library.Utils.Exports;

namespace PrimeBakes.Library.Store.Sale.Exports;

public static class SaleReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<SaleOverviewModel> saleData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		LedgerModel party = null,
		CompanyModel company = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(SaleOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.OrderTransactionNo)] = new() { DisplayName = "Order No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.CustomerName)] = new() { DisplayName = "Customer", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleOverviewModel.OrderDateTime)] = new() { DisplayName = "Order Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(SaleOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(SaleOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(SaleOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, IsRequired = true, IsGrandTotal = true, HighlightNegative = true },

			[nameof(SaleOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.FinancialAccountingTransactionNo)] = new() { DisplayName = "Accounting Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(SaleOverviewModel.LocationName),
				nameof(SaleOverviewModel.TotalItems),
				nameof(SaleOverviewModel.TotalQuantity),
				nameof(SaleOverviewModel.BaseTotal),
				nameof(SaleOverviewModel.ItemDiscountAmount),
				nameof(SaleOverviewModel.TotalAfterItemDiscount),
				nameof(SaleOverviewModel.TotalInclusiveTaxAmount),
				nameof(SaleOverviewModel.TotalExtraTaxAmount),
				nameof(SaleOverviewModel.TotalAfterTax),
				nameof(SaleOverviewModel.OtherChargesAmount),
				nameof(SaleOverviewModel.DiscountAmount),
				nameof(SaleOverviewModel.RoundOffAmount),
				nameof(SaleOverviewModel.TotalAmount),
				nameof(SaleOverviewModel.Cash),
				nameof(SaleOverviewModel.Card),
				nameof(SaleOverviewModel.UPI),
				nameof(SaleOverviewModel.Credit)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(SaleOverviewModel.TransactionNo),
				nameof(SaleOverviewModel.OrderTransactionNo),
				nameof(SaleOverviewModel.CompanyName),
				nameof(SaleOverviewModel.LocationName),
				nameof(SaleOverviewModel.PartyName),
				nameof(SaleOverviewModel.CustomerName),
				nameof(SaleOverviewModel.TransactionDateTime),
				nameof(SaleOverviewModel.OrderDateTime),
				nameof(SaleOverviewModel.FinancialYear),
				nameof(SaleOverviewModel.TotalItems),
				nameof(SaleOverviewModel.TotalQuantity),
				nameof(SaleOverviewModel.BaseTotal),
				nameof(SaleOverviewModel.ItemDiscountAmount),
				nameof(SaleOverviewModel.TotalAfterItemDiscount),
				nameof(SaleOverviewModel.TotalInclusiveTaxAmount),
				nameof(SaleOverviewModel.TotalExtraTaxAmount),
				nameof(SaleOverviewModel.TotalAfterTax),
				nameof(SaleOverviewModel.OtherChargesPercent),
				nameof(SaleOverviewModel.OtherChargesAmount),
				nameof(SaleOverviewModel.DiscountPercent),
				nameof(SaleOverviewModel.DiscountAmount),
				nameof(SaleOverviewModel.RoundOffAmount),
				nameof(SaleOverviewModel.TotalAmount),
				nameof(SaleOverviewModel.Cash),
				nameof(SaleOverviewModel.Card),
				nameof(SaleOverviewModel.UPI),
				nameof(SaleOverviewModel.Credit),
				nameof(SaleOverviewModel.PaymentModes),
				nameof(SaleOverviewModel.Remarks),
				nameof(SaleOverviewModel.FinancialAccountingTransactionNo),
				nameof(SaleOverviewModel.CreatedByName),
				nameof(SaleOverviewModel.CreatedAt),
				nameof(SaleOverviewModel.CreatedFromPlatform),
				nameof(SaleOverviewModel.LastModifiedByUserName),
				nameof(SaleOverviewModel.LastModifiedAt),
				nameof(SaleOverviewModel.LastModifiedFromPlatform),
				nameof(SaleOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(SaleOverviewModel.Status));
		}

		else
		{
			columnOrder =
			[
				nameof(SaleOverviewModel.TransactionNo),
				nameof(SaleOverviewModel.OrderTransactionNo),
				nameof(SaleOverviewModel.LocationName),
				nameof(SaleOverviewModel.PartyName),
				nameof(SaleOverviewModel.TransactionDateTime),
				nameof(SaleOverviewModel.TotalQuantity),
				nameof(SaleOverviewModel.TotalAfterTax),
				nameof(SaleOverviewModel.DiscountPercent),
				nameof(SaleOverviewModel.DiscountAmount),
				nameof(SaleOverviewModel.TotalAmount),
				nameof(SaleOverviewModel.PaymentModes),
				nameof(SaleOverviewModel.Status)
			];

			if (location is not null)
				columnOrder.Remove(nameof(SaleOverviewModel.LocationName));

			if (party is not null)
				columnOrder.Remove(nameof(SaleOverviewModel.PartyName));

			if (!showDeleted)
				columnOrder.Remove(nameof(SaleOverviewModel.Status));
		}

		string fileName = $"SALE_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				saleData,
				"SALE REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new()
				{
					["Company"] = company?.Name ?? null,
					["Location"] = location?.Name ?? null,
					["Party"] = party?.Name ?? null
				}
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				saleData,
				"SALE REPORT",
				"Sale Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new()
				{
					["Company"] = company?.Name ?? null,
					["Location"] = location?.Name ?? null,
					["Party"] = party?.Name ?? null
				}
			);

			return (stream, fileName + ".xlsx");
		}
	}

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<SaleItemOverviewModel> saleItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showDeleted = false,
		bool showSummary = false,
		ProductModel product = null,
		ProductCategoryModel productCategory = null,
		CompanyModel company = null,
		LocationModel location = null,
		LedgerModel party = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(SaleItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.ItemBaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(SaleItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl. Tax", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(SaleItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(SaleItemOverviewModel.ItemRemarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.OrderTransactionNo)] = new() { DisplayName = "Order No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.CustomerName)] = new() { DisplayName = "Customer", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.OrderDateTime)] = new() { DisplayName = "Order Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(SaleItemOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(SaleItemOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.SaleDiscountPercent)] = new() { DisplayName = "Bill Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.SaleDiscountAmount)] = new() { DisplayName = "Bill Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(SaleItemOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(SaleItemOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleItemOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleItemOverviewModel.Remarks)] = new() { DisplayName = "Sale Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.FinancialAccountingTransactionNo)] = new() { DisplayName = "Accounting Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleItemOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(SaleItemOverviewModel.ItemName),
				nameof(SaleItemOverviewModel.ItemCode),
				nameof(SaleItemOverviewModel.ItemCategoryName),
				nameof(SaleItemOverviewModel.Quantity),
				nameof(SaleItemOverviewModel.ItemBaseTotal),
				nameof(SaleItemOverviewModel.DiscountAmount),
				nameof(SaleItemOverviewModel.AfterDiscount),
				nameof(SaleItemOverviewModel.SGSTAmount),
				nameof(SaleItemOverviewModel.CGSTAmount),
				nameof(SaleItemOverviewModel.IGSTAmount),
				nameof(SaleItemOverviewModel.TotalTaxAmount),
				nameof(SaleItemOverviewModel.Total),
				nameof(SaleItemOverviewModel.NetTotal)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(SaleItemOverviewModel.ItemName),
				nameof(SaleItemOverviewModel.ItemCode),
				nameof(SaleItemOverviewModel.ItemCategoryName),

				nameof(SaleItemOverviewModel.Quantity),
				nameof(SaleItemOverviewModel.Rate),
				nameof(SaleItemOverviewModel.ItemBaseTotal),
				nameof(SaleItemOverviewModel.DiscountPercent),
				nameof(SaleItemOverviewModel.DiscountAmount),
				nameof(SaleItemOverviewModel.AfterDiscount),

				nameof(SaleItemOverviewModel.CGSTPercent),
				nameof(SaleItemOverviewModel.CGSTAmount),
				nameof(SaleItemOverviewModel.SGSTPercent),
				nameof(SaleItemOverviewModel.SGSTAmount),
				nameof(SaleItemOverviewModel.IGSTPercent),
				nameof(SaleItemOverviewModel.IGSTAmount),
				nameof(SaleItemOverviewModel.TotalTaxAmount),
				nameof(SaleItemOverviewModel.InclusiveTax),

				nameof(SaleItemOverviewModel.Total),
				nameof(SaleItemOverviewModel.NetRate),
				nameof(SaleItemOverviewModel.NetTotal),

				nameof(SaleItemOverviewModel.ItemRemarks),

				nameof(SaleItemOverviewModel.TransactionNo),
				nameof(SaleItemOverviewModel.OrderTransactionNo),
				nameof(SaleItemOverviewModel.CompanyName),
				nameof(SaleItemOverviewModel.LocationName),
				nameof(SaleItemOverviewModel.PartyName),
				nameof(SaleItemOverviewModel.CustomerName),
				nameof(SaleItemOverviewModel.TransactionDateTime),
				nameof(SaleItemOverviewModel.OrderDateTime),
				nameof(SaleItemOverviewModel.FinancialYear),

				nameof(SaleItemOverviewModel.TotalItems),
				nameof(SaleItemOverviewModel.TotalQuantity),
				nameof(SaleItemOverviewModel.BaseTotal),
				nameof(SaleItemOverviewModel.ItemDiscountAmount),
				nameof(SaleItemOverviewModel.TotalAfterItemDiscount),
				nameof(SaleItemOverviewModel.TotalInclusiveTaxAmount),
				nameof(SaleItemOverviewModel.TotalExtraTaxAmount),
				nameof(SaleItemOverviewModel.TotalAfterTax),

				nameof(SaleItemOverviewModel.OtherChargesPercent),
				nameof(SaleItemOverviewModel.OtherChargesAmount),
				nameof(SaleItemOverviewModel.SaleDiscountPercent),
				nameof(SaleItemOverviewModel.SaleDiscountAmount),

				nameof(SaleItemOverviewModel.RoundOffAmount),
				nameof(SaleItemOverviewModel.TotalAmount),

				nameof(SaleItemOverviewModel.Cash),
				nameof(SaleItemOverviewModel.Card),
				nameof(SaleItemOverviewModel.UPI),
				nameof(SaleItemOverviewModel.Credit),
				nameof(SaleItemOverviewModel.PaymentModes),

				nameof(SaleItemOverviewModel.Remarks),
				nameof(SaleItemOverviewModel.FinancialAccountingTransactionNo),
				nameof(SaleItemOverviewModel.CreatedByName),
				nameof(SaleItemOverviewModel.CreatedAt),
				nameof(SaleItemOverviewModel.CreatedFromPlatform),
				nameof(SaleItemOverviewModel.LastModifiedByUserName),
				nameof(SaleItemOverviewModel.LastModifiedAt),
				nameof(SaleItemOverviewModel.LastModifiedFromPlatform),

				nameof(SaleItemOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(SaleItemOverviewModel.MasterStatus));
		}

		else
		{
			columnOrder =
			[
				nameof(SaleItemOverviewModel.ItemName),
				nameof(SaleItemOverviewModel.Quantity),
				nameof(SaleItemOverviewModel.Rate),
				nameof(SaleItemOverviewModel.NetRate),
				nameof(SaleItemOverviewModel.NetTotal),
				nameof(SaleItemOverviewModel.TransactionNo),
				nameof(SaleItemOverviewModel.TransactionDateTime),
				nameof(SaleItemOverviewModel.LocationName),
				nameof(SaleItemOverviewModel.PartyName),
				nameof(SaleItemOverviewModel.OrderTransactionNo),
				nameof(SaleItemOverviewModel.Total),
				nameof(SaleItemOverviewModel.PaymentModes)
			];

			if (product is not null)
				columnOrder.Remove(nameof(SaleItemOverviewModel.ItemName));

			if (location is not null)
				columnOrder.Remove(nameof(SaleItemOverviewModel.LocationName));

			if (party is not null)
				columnOrder.Remove(nameof(SaleItemOverviewModel.PartyName));
		}

		string fileName = $"SALE_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				saleItemData,
				"SALE ITEM REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new()
				{
					["Item"] = product?.Name ?? null,
					["Category"] = productCategory?.Name ?? null,
					["Company"] = company?.Name ?? null,
					["Location"] = location?.Name ?? null,
					["Party"] = party?.Name ?? null
				}
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				saleItemData,
				"SALE ITEM REPORT",
				"Sale Item Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new()
				{
					["Item"] = product?.Name ?? null,
					["Category"] = productCategory?.Name ?? null,
					["Company"] = company?.Name ?? null,
					["Location"] = location?.Name ?? null,
					["Party"] = party?.Name ?? null
				}
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

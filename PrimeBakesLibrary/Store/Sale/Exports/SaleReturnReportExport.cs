using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Store.Sale.Models;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Store.Sale.Exports;

public static class SaleReturnReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<SaleReturnOverviewModel> saleReturnData,
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
			[nameof(SaleReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.CustomerName)] = new() { DisplayName = "Customer", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(SaleReturnOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(SaleReturnOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(SaleReturnOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, IsRequired = true, IsGrandTotal = true, HighlightNegative = true },

			[nameof(SaleReturnOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.FinancialAccountingTransactionNo)] = new() { DisplayName = "Accounting Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleReturnOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(SaleReturnOverviewModel.PartyName),
				nameof(SaleReturnOverviewModel.TotalItems),
				nameof(SaleReturnOverviewModel.TotalQuantity),
				nameof(SaleReturnOverviewModel.BaseTotal),
				nameof(SaleReturnOverviewModel.ItemDiscountAmount),
				nameof(SaleReturnOverviewModel.TotalAfterItemDiscount),
				nameof(SaleReturnOverviewModel.TotalInclusiveTaxAmount),
				nameof(SaleReturnOverviewModel.TotalExtraTaxAmount),
				nameof(SaleReturnOverviewModel.TotalAfterTax),
				nameof(SaleReturnOverviewModel.OtherChargesAmount),
				nameof(SaleReturnOverviewModel.DiscountAmount),
				nameof(SaleReturnOverviewModel.RoundOffAmount),
				nameof(SaleReturnOverviewModel.TotalAmount),
				nameof(SaleReturnOverviewModel.Cash),
				nameof(SaleReturnOverviewModel.Card),
				nameof(SaleReturnOverviewModel.UPI),
				nameof(SaleReturnOverviewModel.Credit)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(SaleReturnOverviewModel.TransactionNo),
				nameof(SaleReturnOverviewModel.CompanyName),
				nameof(SaleReturnOverviewModel.LocationName),
				nameof(SaleReturnOverviewModel.PartyName),
				nameof(SaleReturnOverviewModel.CustomerName),
				nameof(SaleReturnOverviewModel.TransactionDateTime),
				nameof(SaleReturnOverviewModel.FinancialYear),
				nameof(SaleReturnOverviewModel.TotalItems),
				nameof(SaleReturnOverviewModel.TotalQuantity),
				nameof(SaleReturnOverviewModel.BaseTotal),
				nameof(SaleReturnOverviewModel.ItemDiscountAmount),
				nameof(SaleReturnOverviewModel.TotalAfterItemDiscount),
				nameof(SaleReturnOverviewModel.TotalInclusiveTaxAmount),
				nameof(SaleReturnOverviewModel.TotalExtraTaxAmount),
				nameof(SaleReturnOverviewModel.TotalAfterTax),
				nameof(SaleReturnOverviewModel.OtherChargesPercent),
				nameof(SaleReturnOverviewModel.OtherChargesAmount),
				nameof(SaleReturnOverviewModel.DiscountPercent),
				nameof(SaleReturnOverviewModel.DiscountAmount),
				nameof(SaleReturnOverviewModel.RoundOffAmount),
				nameof(SaleReturnOverviewModel.TotalAmount),
				nameof(SaleReturnOverviewModel.Cash),
				nameof(SaleReturnOverviewModel.Card),
				nameof(SaleReturnOverviewModel.UPI),
				nameof(SaleReturnOverviewModel.Credit),
				nameof(SaleReturnOverviewModel.PaymentModes),
				nameof(SaleReturnOverviewModel.Remarks),
				nameof(SaleReturnOverviewModel.FinancialAccountingTransactionNo),
				nameof(SaleReturnOverviewModel.CreatedByName),
				nameof(SaleReturnOverviewModel.CreatedAt),
				nameof(SaleReturnOverviewModel.CreatedFromPlatform),
				nameof(SaleReturnOverviewModel.LastModifiedByUserName),
				nameof(SaleReturnOverviewModel.LastModifiedAt),
				nameof(SaleReturnOverviewModel.LastModifiedFromPlatform),
				nameof(SaleReturnOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(SaleReturnOverviewModel.Status));
		}

		else
		{
			columnOrder =
			[
				nameof(SaleReturnOverviewModel.TransactionNo),
				nameof(SaleReturnOverviewModel.LocationName),
				nameof(SaleReturnOverviewModel.PartyName),
				nameof(SaleReturnOverviewModel.TransactionDateTime),
				nameof(SaleReturnOverviewModel.TotalQuantity),
				nameof(SaleReturnOverviewModel.TotalAfterTax),
				nameof(SaleReturnOverviewModel.DiscountPercent),
				nameof(SaleReturnOverviewModel.DiscountAmount),
				nameof(SaleReturnOverviewModel.TotalAmount),
				nameof(SaleReturnOverviewModel.PaymentModes),
				nameof(SaleReturnOverviewModel.Status)
			];

			if (location is not null)
				columnOrder.Remove(nameof(SaleReturnOverviewModel.LocationName));

			if (party is not null)
				columnOrder.Remove(nameof(SaleReturnOverviewModel.PartyName));

			if (!showDeleted)
				columnOrder.Remove(nameof(SaleReturnOverviewModel.Status));
		}

		string fileName = $"SALE_RETURN_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				saleReturnData,
				"SALE RETURN REPORT",
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
				saleReturnData,
				"SALE RETURN REPORT",
				"Sale Return Transactions",
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
		IEnumerable<SaleReturnItemOverviewModel> saleReturnItemData,
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
			[nameof(SaleReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.ItemRemarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleReturnItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.ItemBaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl. Tax", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(SaleReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.CustomerName)] = new() { DisplayName = "Customer", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(SaleReturnItemOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Item Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(SaleReturnItemOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.SaleReturnDiscountPercent)] = new() { DisplayName = "Bill Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.SaleReturnDiscountAmount)] = new() { DisplayName = "Bill Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(SaleReturnItemOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(SaleReturnItemOverviewModel.Cash)] = new() { DisplayName = "Cash", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.Card)] = new() { DisplayName = "Card", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.UPI)] = new() { DisplayName = "UPI", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.Credit)] = new() { DisplayName = "Credit", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(SaleReturnItemOverviewModel.PaymentModes)] = new() { DisplayName = "Payment Modes", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleReturnItemOverviewModel.Remarks)] = new() { DisplayName = "Sale Return Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.FinancialAccountingTransactionNo)] = new() { DisplayName = "Accounting Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleReturnItemOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleReturnItemOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(SaleReturnItemOverviewModel.ItemName),
				nameof(SaleReturnItemOverviewModel.ItemCode),
				nameof(SaleReturnItemOverviewModel.ItemCategoryName),
				nameof(SaleReturnItemOverviewModel.Quantity),
				nameof(SaleReturnItemOverviewModel.ItemBaseTotal),
				nameof(SaleReturnItemOverviewModel.DiscountAmount),
				nameof(SaleReturnItemOverviewModel.AfterDiscount),
				nameof(SaleReturnItemOverviewModel.SGSTAmount),
				nameof(SaleReturnItemOverviewModel.CGSTAmount),
				nameof(SaleReturnItemOverviewModel.IGSTAmount),
				nameof(SaleReturnItemOverviewModel.TotalTaxAmount),
				nameof(SaleReturnItemOverviewModel.Total),
				nameof(SaleReturnItemOverviewModel.NetTotal)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(SaleReturnItemOverviewModel.ItemName),
				nameof(SaleReturnItemOverviewModel.ItemCode),
				nameof(SaleReturnItemOverviewModel.ItemCategoryName),
				nameof(SaleReturnItemOverviewModel.ItemRemarks),

				nameof(SaleReturnItemOverviewModel.Quantity),
				nameof(SaleReturnItemOverviewModel.Rate),
				nameof(SaleReturnItemOverviewModel.NetRate),
				nameof(SaleReturnItemOverviewModel.ItemBaseTotal),
				nameof(SaleReturnItemOverviewModel.DiscountPercent),
				nameof(SaleReturnItemOverviewModel.DiscountAmount),
				nameof(SaleReturnItemOverviewModel.AfterDiscount),
				nameof(SaleReturnItemOverviewModel.SGSTPercent),
				nameof(SaleReturnItemOverviewModel.SGSTAmount),
				nameof(SaleReturnItemOverviewModel.CGSTPercent),
				nameof(SaleReturnItemOverviewModel.CGSTAmount),
				nameof(SaleReturnItemOverviewModel.IGSTPercent),
				nameof(SaleReturnItemOverviewModel.IGSTAmount),
				nameof(SaleReturnItemOverviewModel.TotalTaxAmount),
				nameof(SaleReturnItemOverviewModel.InclusiveTax),
				nameof(SaleReturnItemOverviewModel.Total),
				nameof(SaleReturnItemOverviewModel.NetTotal),

				nameof(SaleReturnItemOverviewModel.TransactionNo),
				nameof(SaleReturnItemOverviewModel.CompanyName),
				nameof(SaleReturnItemOverviewModel.LocationName),
				nameof(SaleReturnItemOverviewModel.PartyName),
				nameof(SaleReturnItemOverviewModel.CustomerName),
				nameof(SaleReturnItemOverviewModel.TransactionDateTime),
				nameof(SaleReturnItemOverviewModel.FinancialYear),

				nameof(SaleReturnItemOverviewModel.TotalItems),
				nameof(SaleReturnItemOverviewModel.TotalQuantity),
				nameof(SaleReturnItemOverviewModel.BaseTotal),
				nameof(SaleReturnItemOverviewModel.ItemDiscountAmount),
				nameof(SaleReturnItemOverviewModel.TotalAfterItemDiscount),
				nameof(SaleReturnItemOverviewModel.TotalInclusiveTaxAmount),
				nameof(SaleReturnItemOverviewModel.TotalExtraTaxAmount),
				nameof(SaleReturnItemOverviewModel.TotalAfterTax),

				nameof(SaleReturnItemOverviewModel.OtherChargesPercent),
				nameof(SaleReturnItemOverviewModel.OtherChargesAmount),
				nameof(SaleReturnItemOverviewModel.SaleReturnDiscountPercent),
				nameof(SaleReturnItemOverviewModel.SaleReturnDiscountAmount),

				nameof(SaleReturnItemOverviewModel.RoundOffAmount),
				nameof(SaleReturnItemOverviewModel.TotalAmount),

				nameof(SaleReturnItemOverviewModel.Cash),
				nameof(SaleReturnItemOverviewModel.Card),
				nameof(SaleReturnItemOverviewModel.UPI),
				nameof(SaleReturnItemOverviewModel.Credit),
				nameof(SaleReturnItemOverviewModel.PaymentModes),

				nameof(SaleReturnItemOverviewModel.Remarks),
				nameof(SaleReturnItemOverviewModel.FinancialAccountingTransactionNo),
				nameof(SaleReturnItemOverviewModel.CreatedByName),
				nameof(SaleReturnItemOverviewModel.CreatedAt),
				nameof(SaleReturnItemOverviewModel.CreatedFromPlatform),
				nameof(SaleReturnItemOverviewModel.LastModifiedByUserName),
				nameof(SaleReturnItemOverviewModel.LastModifiedAt),
				nameof(SaleReturnItemOverviewModel.LastModifiedFromPlatform),

				nameof(SaleReturnItemOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(SaleReturnItemOverviewModel.MasterStatus));
		}

		else
		{
			columnOrder =
			[
				nameof(SaleReturnItemOverviewModel.ItemName),
				nameof(SaleReturnItemOverviewModel.Quantity),
				nameof(SaleReturnItemOverviewModel.Rate),
				nameof(SaleReturnItemOverviewModel.NetRate),
				nameof(SaleReturnItemOverviewModel.NetTotal),
				nameof(SaleReturnItemOverviewModel.TransactionNo),
				nameof(SaleReturnItemOverviewModel.TransactionDateTime),
				nameof(SaleReturnItemOverviewModel.LocationName),
				nameof(SaleReturnItemOverviewModel.PartyName)
			];

			if (product is not null)
				columnOrder.Remove(nameof(SaleReturnItemOverviewModel.ItemName));

			if (location is not null)
				columnOrder.Remove(nameof(SaleReturnItemOverviewModel.LocationName));

			if (party is not null)
				columnOrder.Remove(nameof(SaleReturnItemOverviewModel.PartyName));
		}

		string fileName = $"SALE_RETURN_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				saleReturnItemData,
				"SALE RETURN ITEM REPORT",
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
				saleReturnItemData,
				"SALE RETURN ITEM REPORT",
				"Sale Return Item Transactions",
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

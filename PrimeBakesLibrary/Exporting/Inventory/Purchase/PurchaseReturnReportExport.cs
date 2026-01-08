using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

public static class PurchaseReturnReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<PurchaseReturnOverviewModel> purchaseReturnData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		LedgerModel party = null,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(PurchaseReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseReturnOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true },
			[nameof(PurchaseReturnOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Dis Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.CashDiscountPercent)] = new() { DisplayName = "Cash Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.CashDiscountAmount)] = new() { DisplayName = "Cash Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true }
		};

		List<string> columnOrder;

		if (showSummary)
		{
			columnOrder =
			[
				nameof(PurchaseReturnOverviewModel.PartyName),
				nameof(PurchaseReturnOverviewModel.TotalItems),
				nameof(PurchaseReturnOverviewModel.TotalQuantity),
				nameof(PurchaseReturnOverviewModel.BaseTotal),
				nameof(PurchaseReturnOverviewModel.ItemDiscountAmount),
				nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount),
				nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount),
				nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount),
				nameof(PurchaseReturnOverviewModel.TotalAfterTax),
				nameof(PurchaseReturnOverviewModel.CashDiscountAmount),
				nameof(PurchaseReturnOverviewModel.OtherChargesAmount),
				nameof(PurchaseReturnOverviewModel.RoundOffAmount),
				nameof(PurchaseReturnOverviewModel.TotalAmount)
			];

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseReturnOverviewModel.PartyName));
		}

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(PurchaseReturnOverviewModel.TransactionNo),
				nameof(PurchaseReturnOverviewModel.TransactionDateTime),
				nameof(PurchaseReturnOverviewModel.PartyName),
				nameof(PurchaseReturnOverviewModel.CompanyName),
				nameof(PurchaseReturnOverviewModel.FinancialYear),
				nameof(PurchaseReturnOverviewModel.TotalItems),
				nameof(PurchaseReturnOverviewModel.TotalQuantity),
				nameof(PurchaseReturnOverviewModel.BaseTotal),
				nameof(PurchaseReturnOverviewModel.ItemDiscountAmount),
				nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount),
				nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount),
				nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount),
				nameof(PurchaseReturnOverviewModel.TotalAfterTax),
				nameof(PurchaseReturnOverviewModel.OtherChargesPercent),
				nameof(PurchaseReturnOverviewModel.OtherChargesAmount),
				nameof(PurchaseReturnOverviewModel.CashDiscountPercent),
				nameof(PurchaseReturnOverviewModel.CashDiscountAmount),
				nameof(PurchaseReturnOverviewModel.RoundOffAmount),
				nameof(PurchaseReturnOverviewModel.TotalAmount),
				nameof(PurchaseReturnOverviewModel.Remarks),
				nameof(PurchaseReturnOverviewModel.CreatedByName),
				nameof(PurchaseReturnOverviewModel.CreatedAt),
				nameof(PurchaseReturnOverviewModel.CreatedFromPlatform),
				nameof(PurchaseReturnOverviewModel.LastModifiedByUserName),
				nameof(PurchaseReturnOverviewModel.LastModifiedAt),
				nameof(PurchaseReturnOverviewModel.LastModifiedFromPlatform)
			];

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseReturnOverviewModel.PartyName));

			if (company is not null)
				columnOrder.Remove(nameof(PurchaseReturnOverviewModel.CompanyName));
		}

		else
		{
			columnOrder =
			[
				nameof(PurchaseReturnOverviewModel.PartyName),
				nameof(PurchaseReturnOverviewModel.TransactionNo),
				nameof(PurchaseReturnOverviewModel.TransactionDateTime),
				nameof(PurchaseReturnOverviewModel.TotalQuantity),
				nameof(PurchaseReturnOverviewModel.TotalAfterTax),
				nameof(PurchaseReturnOverviewModel.OtherChargesPercent),
				nameof(PurchaseReturnOverviewModel.CashDiscountPercent),
				nameof(PurchaseReturnOverviewModel.TotalAmount)
			];

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseReturnOverviewModel.PartyName));
		}

		string fileName = $"PURCHASE_RETURN_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				purchaseReturnData,
				"PURCHASE RETURN REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new() { ["Company"] = company?.Name ?? null, ["Party"] = party?.Name ?? null }
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				purchaseReturnData,
				"PURCHASE RETURN REPORT",
				"Purchase Return Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Company"] = company?.Name ?? null, ["Party"] = party?.Name ?? null }
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<PurchaseReturnItemOverviewModel> purchaseReturnItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		LedgerModel party = null,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(PurchaseReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.PurchaseReturnRemarks)] = new() { DisplayName = "Purchase Return Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true }
		};

		List<string> columnOrder;

		if (showSummary)
		{
			columnOrder =
			[
				nameof(PurchaseReturnItemOverviewModel.ItemName),
				nameof(PurchaseReturnItemOverviewModel.ItemCode),
				nameof(PurchaseReturnItemOverviewModel.ItemCategoryName),
				nameof(PurchaseReturnItemOverviewModel.Quantity),
				nameof(PurchaseReturnItemOverviewModel.BaseTotal),
				nameof(PurchaseReturnItemOverviewModel.DiscountAmount),
				nameof(PurchaseReturnItemOverviewModel.AfterDiscount),
				nameof(PurchaseReturnItemOverviewModel.SGSTAmount),
				nameof(PurchaseReturnItemOverviewModel.CGSTAmount),
				nameof(PurchaseReturnItemOverviewModel.IGSTAmount),
				nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount),
				nameof(PurchaseReturnItemOverviewModel.Total),
				nameof(PurchaseReturnItemOverviewModel.NetTotal)
			];
		}

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(PurchaseReturnItemOverviewModel.ItemName),
				nameof(PurchaseReturnItemOverviewModel.ItemCode),
				nameof(PurchaseReturnItemOverviewModel.ItemCategoryName),
				nameof(PurchaseReturnItemOverviewModel.TransactionNo),
				nameof(PurchaseReturnItemOverviewModel.TransactionDateTime),
				nameof(PurchaseReturnItemOverviewModel.CompanyName),
				nameof(PurchaseReturnItemOverviewModel.PartyName),
				nameof(PurchaseReturnItemOverviewModel.Quantity),
				nameof(PurchaseReturnItemOverviewModel.Rate),
				nameof(PurchaseReturnItemOverviewModel.BaseTotal),
				nameof(PurchaseReturnItemOverviewModel.DiscountPercent),
				nameof(PurchaseReturnItemOverviewModel.DiscountAmount),
				nameof(PurchaseReturnItemOverviewModel.AfterDiscount),
				nameof(PurchaseReturnItemOverviewModel.SGSTPercent),
				nameof(PurchaseReturnItemOverviewModel.SGSTAmount),
				nameof(PurchaseReturnItemOverviewModel.CGSTPercent),
				nameof(PurchaseReturnItemOverviewModel.CGSTAmount),
				nameof(PurchaseReturnItemOverviewModel.IGSTPercent),
				nameof(PurchaseReturnItemOverviewModel.IGSTAmount),
				nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount),
				nameof(PurchaseReturnItemOverviewModel.InclusiveTax),
				nameof(PurchaseReturnItemOverviewModel.Total),
				nameof(PurchaseReturnItemOverviewModel.NetRate),
				nameof(PurchaseReturnItemOverviewModel.NetTotal),
				nameof(PurchaseReturnItemOverviewModel.PurchaseReturnRemarks),
				nameof(PurchaseReturnItemOverviewModel.Remarks)
			];

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseReturnItemOverviewModel.PartyName));

			if (company is not null)
				columnOrder.Remove(nameof(PurchaseReturnItemOverviewModel.CompanyName));
		}

		else
		{
			columnOrder =
			[
				nameof(PurchaseReturnItemOverviewModel.ItemName),
				nameof(PurchaseReturnItemOverviewModel.ItemCode),
				nameof(PurchaseReturnItemOverviewModel.TransactionNo),
				nameof(PurchaseReturnItemOverviewModel.TransactionDateTime),
				nameof(PurchaseReturnItemOverviewModel.PartyName),
				nameof(PurchaseReturnItemOverviewModel.Quantity),
				nameof(PurchaseReturnItemOverviewModel.NetRate),
				nameof(PurchaseReturnItemOverviewModel.NetTotal)
			];

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseReturnItemOverviewModel.PartyName));
		}

		string fileName = $"PURCHASE_RETURN_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				purchaseReturnItemData,
				"PURCHASE RETURN ITEM REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new() { ["Company"] = company?.Name ?? null, ["Party"] = party?.Name ?? null }
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				purchaseReturnItemData,
				"PURCHASE RETURN ITEM REPORT",
				"Purchase Return Item Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Company"] = company?.Name ?? null, ["Party"] = party?.Name ?? null }
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}

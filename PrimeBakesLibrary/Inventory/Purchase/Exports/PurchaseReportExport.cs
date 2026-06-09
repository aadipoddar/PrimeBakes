using PrimeBakesLibrary.Accounts.Masters.Models;
using PrimeBakesLibrary.Inventory.Purchase.Models;
using PrimeBakesLibrary.Inventory.RawMaterial.Models;
using PrimeBakesLibrary.Utils.Exports;

namespace PrimeBakesLibrary.Inventory.Purchase.Exports;

public static class PurchaseReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<PurchaseOverviewModel> purchaseData,
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
			[nameof(PurchaseOverviewModel.TransactionNo)] = new() { DisplayName = "Transa No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(PurchaseOverviewModel.ChallanNo)] = new() { DisplayName = "Challan", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Dis Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(PurchaseOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseOverviewModel.CashDiscountPercent)] = new() { DisplayName = "Cash Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseOverviewModel.CashDiscountAmount)] = new() { DisplayName = "Cash Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(PurchaseOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(PurchaseOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOverviewModel.FinancialAccountingTransactionNo)] = new() { DisplayName = "Accounting Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(PurchaseOverviewModel.PartyName),
				nameof(PurchaseOverviewModel.TotalItems),
				nameof(PurchaseOverviewModel.TotalQuantity),
				nameof(PurchaseOverviewModel.BaseTotal),
				nameof(PurchaseOverviewModel.ItemDiscountAmount),
				nameof(PurchaseOverviewModel.TotalAfterItemDiscount),
				nameof(PurchaseOverviewModel.TotalInclusiveTaxAmount),
				nameof(PurchaseOverviewModel.TotalExtraTaxAmount),
				nameof(PurchaseOverviewModel.TotalAfterTax),
				nameof(PurchaseOverviewModel.CashDiscountAmount),
				nameof(PurchaseOverviewModel.OtherChargesAmount),
				nameof(PurchaseOverviewModel.RoundOffAmount),
				nameof(PurchaseOverviewModel.TotalAmount)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(PurchaseOverviewModel.TransactionNo),
				nameof(PurchaseOverviewModel.CompanyName),
				nameof(PurchaseOverviewModel.TransactionDateTime),
				nameof(PurchaseOverviewModel.FinancialYear),
				nameof(PurchaseOverviewModel.ChallanNo),
				nameof(PurchaseOverviewModel.PartyName),
				nameof(PurchaseOverviewModel.TotalItems),
				nameof(PurchaseOverviewModel.TotalQuantity),
				nameof(PurchaseOverviewModel.BaseTotal),
				nameof(PurchaseOverviewModel.ItemDiscountAmount),
				nameof(PurchaseOverviewModel.TotalAfterItemDiscount),
				nameof(PurchaseOverviewModel.TotalInclusiveTaxAmount),
				nameof(PurchaseOverviewModel.TotalExtraTaxAmount),
				nameof(PurchaseOverviewModel.TotalAfterTax),
				nameof(PurchaseOverviewModel.OtherChargesPercent),
				nameof(PurchaseOverviewModel.OtherChargesAmount),
				nameof(PurchaseOverviewModel.CashDiscountPercent),
				nameof(PurchaseOverviewModel.CashDiscountAmount),
				nameof(PurchaseOverviewModel.RoundOffAmount),
				nameof(PurchaseOverviewModel.TotalAmount),
				nameof(PurchaseOverviewModel.Remarks),
				nameof(PurchaseOverviewModel.FinancialAccountingTransactionNo),
				nameof(PurchaseOverviewModel.CreatedByName),
				nameof(PurchaseOverviewModel.CreatedAt),
				nameof(PurchaseOverviewModel.CreatedFromPlatform),
				nameof(PurchaseOverviewModel.LastModifiedByUserName),
				nameof(PurchaseOverviewModel.LastModifiedAt),
				nameof(PurchaseOverviewModel.LastModifiedFromPlatform),
				nameof(PurchaseOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(PurchaseOverviewModel.Status));
		}

		else
		{
			columnOrder =
			[
				nameof(PurchaseOverviewModel.TransactionNo),
				nameof(PurchaseOverviewModel.ChallanNo),
				nameof(PurchaseOverviewModel.PartyName),
				nameof(PurchaseOverviewModel.TransactionDateTime),
				nameof(PurchaseOverviewModel.TotalQuantity),
				nameof(PurchaseOverviewModel.TotalAfterTax),
				nameof(PurchaseOverviewModel.OtherChargesPercent),
				nameof(PurchaseOverviewModel.CashDiscountPercent),
				nameof(PurchaseOverviewModel.TotalAmount),
				nameof(PurchaseOverviewModel.Status)
			];

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseOverviewModel.PartyName));

			if (!showDeleted)
				columnOrder.Remove(nameof(PurchaseOverviewModel.Status));
		}

		string fileName = $"PURCHASE_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				purchaseData,
				"PURCHASE REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new()
				{
					["Company"] = company?.Name ?? null,
					["Party"] = party?.Name ?? null
				}
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				purchaseData,
				"PURCHASE REPORT",
				"Purchase Transactions",
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

			return (stream, fileName + ".xlsx");
		}
	}

	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<PurchaseItemOverviewModel> purchaseItemData,
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
			[nameof(PurchaseItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseItemOverviewModel.UnitOfMeasurement)] = new() { DisplayName = "UOM", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.ItemRemarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.ItemBaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl. Tax", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(PurchaseItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transa No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(PurchaseItemOverviewModel.ChallanNo)] = new() { DisplayName = "Challan", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseItemOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Dis Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(PurchaseItemOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.CashDiscountPercent)] = new() { DisplayName = "Cash Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.CashDiscountAmount)] = new() { DisplayName = "Cash Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(PurchaseItemOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseItemOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(PurchaseItemOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.FinancialAccountingTransactionNo)] = new() { DisplayName = "Accounting Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseItemOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(PurchaseItemOverviewModel.ItemName),
				nameof(PurchaseItemOverviewModel.ItemCode),
				nameof(PurchaseItemOverviewModel.ItemCategoryName),
				nameof(PurchaseItemOverviewModel.Quantity),
				nameof(PurchaseItemOverviewModel.ItemBaseTotal),
				nameof(PurchaseItemOverviewModel.DiscountAmount),
				nameof(PurchaseItemOverviewModel.AfterDiscount),
				nameof(PurchaseItemOverviewModel.SGSTAmount),
				nameof(PurchaseItemOverviewModel.CGSTAmount),
				nameof(PurchaseItemOverviewModel.IGSTAmount),
				nameof(PurchaseItemOverviewModel.TotalTaxAmount),
				nameof(PurchaseItemOverviewModel.Total),
				nameof(PurchaseItemOverviewModel.NetTotal)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(PurchaseItemOverviewModel.ItemName),
				nameof(PurchaseItemOverviewModel.ItemCode),
				nameof(PurchaseItemOverviewModel.ItemCategoryName),
				nameof(PurchaseItemOverviewModel.TransactionNo),
				nameof(PurchaseItemOverviewModel.ChallanNo),
				nameof(PurchaseItemOverviewModel.TransactionDateTime),
				nameof(PurchaseItemOverviewModel.CompanyName),
				nameof(PurchaseItemOverviewModel.PartyName),
				nameof(PurchaseItemOverviewModel.FinancialYear),
				nameof(PurchaseItemOverviewModel.Quantity),
				nameof(PurchaseItemOverviewModel.Rate),
				nameof(PurchaseItemOverviewModel.ItemBaseTotal),
				nameof(PurchaseItemOverviewModel.DiscountPercent),
				nameof(PurchaseItemOverviewModel.DiscountAmount),
				nameof(PurchaseItemOverviewModel.AfterDiscount),
				nameof(PurchaseItemOverviewModel.SGSTPercent),
				nameof(PurchaseItemOverviewModel.SGSTAmount),
				nameof(PurchaseItemOverviewModel.CGSTPercent),
				nameof(PurchaseItemOverviewModel.CGSTAmount),
				nameof(PurchaseItemOverviewModel.IGSTPercent),
				nameof(PurchaseItemOverviewModel.IGSTAmount),
				nameof(PurchaseItemOverviewModel.TotalTaxAmount),
				nameof(PurchaseItemOverviewModel.InclusiveTax),
				nameof(PurchaseItemOverviewModel.Total),
				nameof(PurchaseItemOverviewModel.NetRate),
				nameof(PurchaseItemOverviewModel.NetTotal),
				nameof(PurchaseItemOverviewModel.TotalItems),
				nameof(PurchaseItemOverviewModel.TotalQuantity),
				nameof(PurchaseItemOverviewModel.BaseTotal),
				nameof(PurchaseItemOverviewModel.ItemDiscountAmount),
				nameof(PurchaseItemOverviewModel.TotalAfterItemDiscount),
				nameof(PurchaseItemOverviewModel.TotalInclusiveTaxAmount),
				nameof(PurchaseItemOverviewModel.TotalExtraTaxAmount),
				nameof(PurchaseItemOverviewModel.TotalAfterTax),
				nameof(PurchaseItemOverviewModel.OtherChargesPercent),
				nameof(PurchaseItemOverviewModel.OtherChargesAmount),
				nameof(PurchaseItemOverviewModel.CashDiscountPercent),
				nameof(PurchaseItemOverviewModel.CashDiscountAmount),
				nameof(PurchaseItemOverviewModel.RoundOffAmount),
				nameof(PurchaseItemOverviewModel.TotalAmount),
				nameof(PurchaseItemOverviewModel.Remarks),
				nameof(PurchaseItemOverviewModel.FinancialAccountingTransactionNo),
				nameof(PurchaseItemOverviewModel.ItemRemarks),
				nameof(PurchaseItemOverviewModel.CreatedByName),
				nameof(PurchaseItemOverviewModel.CreatedAt),
				nameof(PurchaseItemOverviewModel.CreatedFromPlatform),
				nameof(PurchaseItemOverviewModel.LastModifiedByUserName),
				nameof(PurchaseItemOverviewModel.LastModifiedAt),
				nameof(PurchaseItemOverviewModel.LastModifiedFromPlatform),
				nameof(PurchaseItemOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(PurchaseOverviewModel.Status));
		}

		else
		{
			columnOrder =
			[
				nameof(PurchaseItemOverviewModel.ItemName),
				nameof(PurchaseItemOverviewModel.Quantity),
				nameof(PurchaseItemOverviewModel.Rate),
				nameof(PurchaseItemOverviewModel.NetRate),
				nameof(PurchaseItemOverviewModel.NetTotal),
				nameof(PurchaseItemOverviewModel.TransactionNo),
				nameof(PurchaseItemOverviewModel.TransactionDateTime),
				nameof(PurchaseItemOverviewModel.ChallanNo),
				nameof(PurchaseItemOverviewModel.PartyName),
			];

			if (rawMaterial is not null)
				columnOrder.Remove(nameof(PurchaseItemOverviewModel.ItemName));

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseItemOverviewModel.PartyName));
		}

		string fileName = $"PURCHASE_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				purchaseItemData,
				"PURCHASE ITEM REPORT",
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
					["Party"] = party?.Name ?? null
				}
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				purchaseItemData,
				"PURCHASE ITEM REPORT",
				"Purchase Item Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new()
				{
					["Raw Material"] = rawMaterial?.Name ?? null,
					["Raw Material Category"] = rawMaterialCategory?.Name ?? null,
					["Company"] = company?.Name ?? null,
					["Party"] = party?.Name ?? null
				}
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

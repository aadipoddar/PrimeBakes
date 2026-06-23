using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Inventory.Purchase.Models;
using PrimeBakes.Library.Inventory.RawMaterial.Models;
using PrimeBakes.Library.Utils.Exports;

namespace PrimeBakes.Library.Inventory.Purchase.Exports;

public static class PurchaseReturnReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<PurchaseReturnOverviewModel> purchaseData,
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
			[nameof(PurchaseReturnOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(PurchaseReturnOverviewModel.ChallanNo)] = new() { DisplayName = "Challan", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseReturnOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Dis Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(PurchaseReturnOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.CashDiscountPercent)] = new() { DisplayName = "Cash Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.CashDiscountAmount)] = new() { DisplayName = "Cash Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(PurchaseReturnOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(PurchaseReturnOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.FinancialAccountingTransactionNo)] = new() { DisplayName = "Accounting Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseReturnOverviewModel.Status)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
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

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(PurchaseReturnOverviewModel.TransactionNo),
				nameof(PurchaseReturnOverviewModel.CompanyName),
				nameof(PurchaseReturnOverviewModel.TransactionDateTime),
				nameof(PurchaseReturnOverviewModel.FinancialYear),
				nameof(PurchaseReturnOverviewModel.ChallanNo),
				nameof(PurchaseReturnOverviewModel.PartyName),
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
				nameof(PurchaseReturnOverviewModel.FinancialAccountingTransactionNo),
				nameof(PurchaseReturnOverviewModel.CreatedByName),
				nameof(PurchaseReturnOverviewModel.CreatedAt),
				nameof(PurchaseReturnOverviewModel.CreatedFromPlatform),
				nameof(PurchaseReturnOverviewModel.LastModifiedByUserName),
				nameof(PurchaseReturnOverviewModel.LastModifiedAt),
				nameof(PurchaseReturnOverviewModel.LastModifiedFromPlatform),
				nameof(PurchaseReturnOverviewModel.Status)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(PurchaseReturnOverviewModel.Status));
		}

		else
		{
			columnOrder =
			[
				nameof(PurchaseReturnOverviewModel.TransactionNo),
				nameof(PurchaseReturnOverviewModel.ChallanNo),
				nameof(PurchaseReturnOverviewModel.PartyName),
				nameof(PurchaseReturnOverviewModel.TransactionDateTime),
				nameof(PurchaseReturnOverviewModel.TotalQuantity),
				nameof(PurchaseReturnOverviewModel.TotalAfterTax),
				nameof(PurchaseReturnOverviewModel.OtherChargesPercent),
				nameof(PurchaseReturnOverviewModel.CashDiscountPercent),
				nameof(PurchaseReturnOverviewModel.TotalAmount),
				nameof(PurchaseReturnOverviewModel.Status)
			];

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseReturnOverviewModel.PartyName));

			if (!showDeleted)
				columnOrder.Remove(nameof(PurchaseReturnOverviewModel.Status));
		}

		string fileName = $"PURCHASE_RETURN_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				purchaseData,
				"PURCHASE RETURN REPORT",
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
				"PURCHASE RETURN REPORT",
				"Purchase Return Transactions",
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
		IEnumerable<PurchaseReturnItemOverviewModel> purchaseItemData,
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
			[nameof(PurchaseReturnItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseReturnItemOverviewModel.UnitOfMeasurement)] = new() { DisplayName = "UOM", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.ItemRemarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseReturnItemOverviewModel.Quantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.Rate)] = new() { DisplayName = "Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.NetRate)] = new() { DisplayName = "Net Rate", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.ItemBaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.DiscountPercent)] = new() { DisplayName = "Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.DiscountAmount)] = new() { DisplayName = "Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.AfterDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.SGSTPercent)] = new() { DisplayName = "SGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.SGSTAmount)] = new() { DisplayName = "SGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.CGSTPercent)] = new() { DisplayName = "CGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.CGSTAmount)] = new() { DisplayName = "CGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.IGSTPercent)] = new() { DisplayName = "IGST %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.IGSTAmount)] = new() { DisplayName = "IGST Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount)] = new() { DisplayName = "Tax Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl. Tax", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.Total)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.NetTotal)] = new() { DisplayName = "Net Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = true, HighlightNegative = true },

			[nameof(PurchaseReturnItemOverviewModel.TransactionNo)] = new() { DisplayName = "Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(PurchaseReturnItemOverviewModel.ChallanNo)] = new() { DisplayName = "Challan", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseReturnItemOverviewModel.TotalItems)] = new() { DisplayName = "Items", Format = "#,##0", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.TotalQuantity)] = new() { DisplayName = "Qty", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.BaseTotal)] = new() { DisplayName = "Base Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.ItemDiscountAmount)] = new() { DisplayName = "Dis Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.TotalAfterItemDiscount)] = new() { DisplayName = "After Disc", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.TotalInclusiveTaxAmount)] = new() { DisplayName = "Incl Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.TotalExtraTaxAmount)] = new() { DisplayName = "Extra Tax", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.TotalAfterTax)] = new() { DisplayName = "Sub Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(PurchaseReturnItemOverviewModel.OtherChargesPercent)] = new() { DisplayName = "Other Charges %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.OtherChargesAmount)] = new() { DisplayName = "Other Charges Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.CashDiscountPercent)] = new() { DisplayName = "Cash Disc %", Format = "#,##0.00", Alignment = CellAlignment.Center, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.CashDiscountAmount)] = new() { DisplayName = "Cash Disc Amt", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(PurchaseReturnItemOverviewModel.RoundOffAmount)] = new() { DisplayName = "Round Off", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },
			[nameof(PurchaseReturnItemOverviewModel.TotalAmount)] = new() { DisplayName = "Total", Format = "#,##0.00", Alignment = CellAlignment.Right, IncludeInTotal = false, HighlightNegative = true },

			[nameof(PurchaseReturnItemOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.FinancialAccountingTransactionNo)] = new() { DisplayName = "Accounting Transaction No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(PurchaseReturnItemOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(PurchaseReturnItemOverviewModel.MasterStatus)] = new() { DisplayName = "Status", Alignment = CellAlignment.Center, IncludeInTotal = false },
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(PurchaseReturnItemOverviewModel.ItemName),
				nameof(PurchaseReturnItemOverviewModel.ItemCode),
				nameof(PurchaseReturnItemOverviewModel.ItemCategoryName),
				nameof(PurchaseReturnItemOverviewModel.Quantity),
				nameof(PurchaseReturnItemOverviewModel.ItemBaseTotal),
				nameof(PurchaseReturnItemOverviewModel.DiscountAmount),
				nameof(PurchaseReturnItemOverviewModel.AfterDiscount),
				nameof(PurchaseReturnItemOverviewModel.SGSTAmount),
				nameof(PurchaseReturnItemOverviewModel.CGSTAmount),
				nameof(PurchaseReturnItemOverviewModel.IGSTAmount),
				nameof(PurchaseReturnItemOverviewModel.TotalTaxAmount),
				nameof(PurchaseReturnItemOverviewModel.Total),
				nameof(PurchaseReturnItemOverviewModel.NetTotal)
			];

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(PurchaseReturnItemOverviewModel.ItemName),
				nameof(PurchaseReturnItemOverviewModel.ItemCode),
				nameof(PurchaseReturnItemOverviewModel.ItemCategoryName),

				nameof(PurchaseReturnItemOverviewModel.UnitOfMeasurement),
				nameof(PurchaseReturnItemOverviewModel.ItemRemarks),

				nameof(PurchaseReturnItemOverviewModel.Quantity),
				nameof(PurchaseReturnItemOverviewModel.Rate),
				nameof(PurchaseReturnItemOverviewModel.NetRate),
				nameof(PurchaseReturnItemOverviewModel.ItemBaseTotal),
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
				nameof(PurchaseReturnItemOverviewModel.NetTotal),

				nameof(PurchaseReturnItemOverviewModel.TransactionNo),
				nameof(PurchaseReturnItemOverviewModel.CompanyName),
				nameof(PurchaseReturnItemOverviewModel.TransactionDateTime),
				nameof(PurchaseReturnItemOverviewModel.FinancialYear),

				nameof(PurchaseReturnItemOverviewModel.ChallanNo),
				nameof(PurchaseReturnItemOverviewModel.PartyName),

				nameof(PurchaseReturnItemOverviewModel.TotalItems),
				nameof(PurchaseReturnItemOverviewModel.TotalQuantity),
				nameof(PurchaseReturnItemOverviewModel.BaseTotal),
				nameof(PurchaseReturnItemOverviewModel.ItemDiscountAmount),
				nameof(PurchaseReturnItemOverviewModel.TotalAfterItemDiscount),
				nameof(PurchaseReturnItemOverviewModel.TotalInclusiveTaxAmount),
				nameof(PurchaseReturnItemOverviewModel.TotalExtraTaxAmount),
				nameof(PurchaseReturnItemOverviewModel.TotalAfterTax),

				nameof(PurchaseReturnItemOverviewModel.OtherChargesPercent),
				nameof(PurchaseReturnItemOverviewModel.OtherChargesAmount),
				nameof(PurchaseReturnItemOverviewModel.CashDiscountPercent),
				nameof(PurchaseReturnItemOverviewModel.CashDiscountAmount),

				nameof(PurchaseReturnItemOverviewModel.RoundOffAmount),
				nameof(PurchaseReturnItemOverviewModel.TotalAmount),

				nameof(PurchaseReturnItemOverviewModel.Remarks),
				nameof(PurchaseReturnItemOverviewModel.FinancialAccountingTransactionNo),
				nameof(PurchaseReturnItemOverviewModel.CreatedAt),
				nameof(PurchaseReturnItemOverviewModel.CreatedByName),
				nameof(PurchaseReturnItemOverviewModel.CreatedFromPlatform),
				nameof(PurchaseReturnItemOverviewModel.LastModifiedAt),
				nameof(PurchaseReturnItemOverviewModel.LastModifiedByUserName),
				nameof(PurchaseReturnItemOverviewModel.LastModifiedFromPlatform),

				nameof(PurchaseReturnItemOverviewModel.MasterStatus)
			];

			if (!showDeleted)
				columnOrder.Remove(nameof(PurchaseReturnItemOverviewModel.MasterStatus));
		}

		else
		{
			columnOrder =
			[
				nameof(PurchaseReturnItemOverviewModel.ItemName),
				nameof(PurchaseReturnItemOverviewModel.Quantity),
				nameof(PurchaseReturnItemOverviewModel.Rate),
				nameof(PurchaseReturnItemOverviewModel.NetRate),
				nameof(PurchaseReturnItemOverviewModel.NetTotal),
				nameof(PurchaseReturnItemOverviewModel.TransactionNo),
				nameof(PurchaseReturnItemOverviewModel.TransactionDateTime),
				nameof(PurchaseReturnItemOverviewModel.ChallanNo),
				nameof(PurchaseReturnItemOverviewModel.PartyName),
			];

			if (rawMaterial is not null)
				columnOrder.Remove(nameof(PurchaseReturnItemOverviewModel.ItemName));

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseReturnItemOverviewModel.PartyName));
		}

		string fileName = $"PURCHASE_RETURN_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				purchaseItemData,
				"PURCHASE RETURN ITEM REPORT",
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
				"PURCHASE RETURN ITEM REPORT",
				"Purchase Return Item Transactions",
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

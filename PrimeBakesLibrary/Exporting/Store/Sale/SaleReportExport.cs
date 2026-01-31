using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.Sale;

namespace PrimeBakesLibrary.Exporting.Store.Sale;

public static class SaleReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<SaleOverviewModel> saleData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		LedgerModel party = null,
		CompanyModel company = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(SaleOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.OrderTransactionNo)] = new() { DisplayName = "Order Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.CustomerName)] = new() { DisplayName = "Customer", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleOverviewModel.OrderDateTime)] = new() { DisplayName = "Order Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(SaleOverviewModel.TotalItems)] = new()
			{
				DisplayName = "Items",
				Format = "#,##0",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.TotalQuantity)] = new()
			{
				DisplayName = "Qty",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.BaseTotal)] = new()
			{
				DisplayName = "Base Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.ItemDiscountAmount)] = new()
			{
				DisplayName = "Item Discount Amount",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.TotalAfterItemDiscount)] = new()
			{
				DisplayName = "After Disc",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleOverviewModel.TotalInclusiveTaxAmount)] = new()
			{
				DisplayName = "Incl Tax",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleOverviewModel.TotalExtraTaxAmount)] = new()
			{
				DisplayName = "Extra Tax",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleOverviewModel.TotalAfterTax)] = new()
			{
				DisplayName = "Sub Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.OtherChargesPercent)] = new()
			{
				DisplayName = "Other Charges %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(SaleOverviewModel.OtherChargesAmount)] = new()
			{
				DisplayName = "Other Charges",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.DiscountPercent)] = new()
			{
				DisplayName = "Disc %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(SaleOverviewModel.DiscountAmount)] = new()
			{
				DisplayName = "Disc Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.RoundOffAmount)] = new()
			{
				DisplayName = "Round Off",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.TotalAmount)] = new()
			{
				DisplayName = "Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				IsRequired = true,
				IsGrandTotal = true
			},

			[nameof(SaleOverviewModel.Cash)] = new()
			{
				DisplayName = "Cash",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.Card)] = new()
			{
				DisplayName = "Card",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.UPI)] = new()
			{
				DisplayName = "UPI",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.Credit)] = new()
			{
				DisplayName = "Credit",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(SaleOverviewModel.PaymentModes)] = new()
			{
				DisplayName = "Payment Modes",
				Alignment = CellAlignment.Left,
				IncludeInTotal = false
			}
		};

		// Define column order based on visibility setting
		List<string> columnOrder;

		// Summary view - grouped by location with totals
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
			// All columns - detailed view
			columnOrder =
			[
				nameof(SaleOverviewModel.TransactionNo),
				nameof(SaleOverviewModel.OrderTransactionNo),
				nameof(SaleOverviewModel.CompanyName)
			];

			if (location is null)
				columnOrder.Add(nameof(SaleOverviewModel.LocationName));

			if (party is null)
				columnOrder.Add(nameof(SaleOverviewModel.PartyName));

			// Continue with remaining columns
			columnOrder.AddRange(
			[
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
				nameof(SaleOverviewModel.CreatedByName),
				nameof(SaleOverviewModel.CreatedAt),
				nameof(SaleOverviewModel.CreatedFromPlatform),
				nameof(SaleOverviewModel.LastModifiedByUserName),
				nameof(SaleOverviewModel.LastModifiedAt),
				nameof(SaleOverviewModel.LastModifiedFromPlatform)
			]);
		}
		else
		{
			// Summary columns - key fields only
			columnOrder =
			[
				nameof(SaleOverviewModel.TransactionNo),
				nameof(SaleOverviewModel.OrderTransactionNo),
				nameof(SaleOverviewModel.TransactionDateTime),
				nameof(SaleOverviewModel.TotalQuantity),
				nameof(SaleOverviewModel.TotalAfterTax),
				nameof(SaleOverviewModel.DiscountPercent),
				nameof(SaleOverviewModel.DiscountAmount),
				nameof(SaleOverviewModel.TotalAmount),
				nameof(SaleOverviewModel.PaymentModes)
			];

			if (location is null)
				columnOrder.Insert(3, nameof(SaleOverviewModel.LocationName));

			if (party is null)
			{
				int insertIndex = location is null ? 4 : 3;
				columnOrder.Insert(insertIndex, nameof(SaleOverviewModel.PartyName));
			}
		}

		if (company is not null)
			columnOrder.Remove(nameof(SaleOverviewModel.CompanyName));

		if (location is not null)
			columnOrder.Remove(nameof(SaleOverviewModel.LocationName));

		if (party is not null)
			columnOrder.Remove(nameof(SaleOverviewModel.PartyName));

		string fileName = "SALE_REPORT";
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
				new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null, ["Party"] = party?.Name ?? null }
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
				new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null, ["Party"] = party?.Name ?? null }
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
		bool showSummary = false,
		LedgerModel party = null,
		CompanyModel company = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(SaleItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.PartyName)] = new() { DisplayName = "Party", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.SaleRemarks)] = new() { DisplayName = "Sale Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(SaleItemOverviewModel.Quantity)] = new()
			{
				DisplayName = "Qty",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleItemOverviewModel.Rate)] = new()
			{
				DisplayName = "Rate",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.NetRate)] = new()
			{
				DisplayName = "Net Rate",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.BaseTotal)] = new()
			{
				DisplayName = "Base Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleItemOverviewModel.DiscountPercent)] = new()
			{
				DisplayName = "Disc %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.DiscountAmount)] = new()
			{
				DisplayName = "Discount Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleItemOverviewModel.AfterDiscount)] = new()
			{
				DisplayName = "After Disc",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleItemOverviewModel.SGSTPercent)] = new()
			{
				DisplayName = "SGST %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.SGSTAmount)] = new()
			{
				DisplayName = "SGST Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleItemOverviewModel.CGSTPercent)] = new()
			{
				DisplayName = "CGST %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.CGSTAmount)] = new()
			{
				DisplayName = "CGST Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleItemOverviewModel.IGSTPercent)] = new()
			{
				DisplayName = "IGST %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.IGSTAmount)] = new()
			{
				DisplayName = "IGST Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleItemOverviewModel.TotalTaxAmount)] = new()
			{
				DisplayName = "Tax",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleItemOverviewModel.Total)] = new()
			{
				DisplayName = "Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(SaleItemOverviewModel.NetTotal)] = new()
			{
				DisplayName = "Net Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			}
		};

		List<string> columnOrder;

		if (showSummary)
			columnOrder =
			[
				nameof(SaleItemOverviewModel.ItemName),
				nameof(SaleItemOverviewModel.ItemCode),
				nameof(SaleItemOverviewModel.ItemCategoryName),
				nameof(SaleItemOverviewModel.Quantity),
				nameof(SaleItemOverviewModel.BaseTotal),
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
			// All columns - detailed view
			List<string> columns =
			[
				nameof(SaleItemOverviewModel.ItemName),
				nameof(SaleItemOverviewModel.ItemCode),
				nameof(SaleItemOverviewModel.ItemCategoryName),
				nameof(SaleItemOverviewModel.TransactionNo),
				nameof(SaleItemOverviewModel.TransactionDateTime),
				nameof(SaleItemOverviewModel.CompanyName)
			];

			if (location is null)
				columns.Add(nameof(SaleItemOverviewModel.LocationName));

			columns.AddRange([
				nameof(SaleItemOverviewModel.PartyName),
				nameof(SaleItemOverviewModel.Quantity),
				nameof(SaleItemOverviewModel.Rate),
				nameof(SaleItemOverviewModel.BaseTotal),
				nameof(SaleItemOverviewModel.DiscountPercent),
				nameof(SaleItemOverviewModel.DiscountAmount),
				nameof(SaleItemOverviewModel.AfterDiscount),
				nameof(SaleItemOverviewModel.SGSTPercent),
				nameof(SaleItemOverviewModel.SGSTAmount),
				nameof(SaleItemOverviewModel.CGSTPercent),
				nameof(SaleItemOverviewModel.CGSTAmount),
				nameof(SaleItemOverviewModel.IGSTPercent),
				nameof(SaleItemOverviewModel.IGSTAmount),
				nameof(SaleItemOverviewModel.TotalTaxAmount),
				nameof(SaleItemOverviewModel.InclusiveTax),
				nameof(SaleItemOverviewModel.Total),
				nameof(SaleItemOverviewModel.NetRate),
				nameof(SaleItemOverviewModel.NetTotal),
				nameof(SaleItemOverviewModel.SaleRemarks),
				nameof(SaleItemOverviewModel.Remarks)
			]);

			columnOrder = columns;
		}
		// Summary columns - key fields only
		else
			columnOrder =
			[
				nameof(SaleItemOverviewModel.ItemName),
				nameof(SaleItemOverviewModel.ItemCode),
				nameof(SaleItemOverviewModel.TransactionNo),
				nameof(SaleItemOverviewModel.TransactionDateTime),
				nameof(SaleItemOverviewModel.LocationName),
				nameof(SaleItemOverviewModel.PartyName),
				nameof(SaleItemOverviewModel.Quantity),
				nameof(SaleItemOverviewModel.NetRate),
				nameof(SaleItemOverviewModel.NetTotal)
			];

		string fileName = "SALE_ITEM_REPORT";
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
				new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null, ["Party"] = party?.Name ?? null }
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
				new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null, ["Party"] = party?.Name ?? null }
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

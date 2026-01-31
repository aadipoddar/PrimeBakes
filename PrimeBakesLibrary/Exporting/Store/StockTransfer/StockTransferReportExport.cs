using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Store.StockTransfer;

namespace PrimeBakesLibrary.Exporting.Store.StockTransfer;

public static class StockTransferReportExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<StockTransferOverviewModel> data,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		CompanyModel company = null,
		LocationModel fromLocation = null,
		LocationModel toLocation = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			// Map display names and formats similar to Excel export
			[nameof(StockTransferOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.LocationName)] = new() { DisplayName = "From Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.ToLocationName)] = new() { DisplayName = "To Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StockTransferOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(StockTransferOverviewModel.TotalItems)] = new()
			{
				DisplayName = "Items",
				Format = "#,##0",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.TotalQuantity)] = new()
			{
				DisplayName = "Qty",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.BaseTotal)] = new()
			{
				DisplayName = "Base Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.ItemDiscountAmount)] = new()
			{
				DisplayName = "Item Disc Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.TotalAfterItemDiscount)] = new()
			{
				DisplayName = "After Disc",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(StockTransferOverviewModel.TotalInclusiveTaxAmount)] = new()
			{
				DisplayName = "Incl Tax",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(StockTransferOverviewModel.TotalExtraTaxAmount)] = new()
			{
				DisplayName = "Extra Tax",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(StockTransferOverviewModel.TotalAfterTax)] = new()
			{
				DisplayName = "Sub Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.OtherChargesPercent)] = new()
			{
				DisplayName = "Other Charges %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(StockTransferOverviewModel.OtherChargesAmount)] = new()
			{
				DisplayName = "Other Charges",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.DiscountPercent)] = new()
			{
				DisplayName = "Disc %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(StockTransferOverviewModel.DiscountAmount)] = new()
			{
				DisplayName = "Disc Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.RoundOffAmount)] = new()
			{
				DisplayName = "Round Off",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.TotalAmount)] = new()
			{
				DisplayName = "Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				IsRequired = true,
				IsGrandTotal = true
			},

			[nameof(StockTransferOverviewModel.Cash)] = new()
			{
				DisplayName = "Cash",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.Card)] = new()
			{
				DisplayName = "Card",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.UPI)] = new()
			{
				DisplayName = "UPI",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.Credit)] = new()
			{
				DisplayName = "Credit",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferOverviewModel.PaymentModes)] = new()
			{
				DisplayName = "Payment Modes",
				Alignment = CellAlignment.Left,
				IncludeInTotal = false
			}
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
				nameof(StockTransferOverviewModel.CreatedByName),
				nameof(StockTransferOverviewModel.CreatedAt),
				nameof(StockTransferOverviewModel.CreatedFromPlatform),
				nameof(StockTransferOverviewModel.LastModifiedByUserName),
				nameof(StockTransferOverviewModel.LastModifiedAt),
				nameof(StockTransferOverviewModel.LastModifiedFromPlatform)
			];
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
				nameof(StockTransferOverviewModel.PaymentModes)
			];
		}

		if (company is not null)
			columnOrder.Remove(nameof(StockTransferOverviewModel.CompanyName));

		if (fromLocation is not null)
			columnOrder.Remove(nameof(StockTransferOverviewModel.LocationName));

		if (toLocation is not null)
			columnOrder.Remove(nameof(StockTransferOverviewModel.ToLocationName));

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
		bool showSummary = false,
		CompanyModel company = null,
		LocationModel fromLocation = null,
		LocationModel toLocation = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(StockTransferItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.LocationName)] = new() { DisplayName = "From Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.ToLocationName)] = new() { DisplayName = "To Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.StockTransferRemarks)] = new() { DisplayName = "Stock Transfer Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(StockTransferItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(StockTransferItemOverviewModel.Quantity)] = new()
			{
				DisplayName = "Qty",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferItemOverviewModel.Rate)] = new()
			{
				DisplayName = "Rate",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = false
			},

			[nameof(StockTransferItemOverviewModel.NetRate)] = new()
			{
				DisplayName = "Net Rate",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = false
			},

			[nameof(StockTransferItemOverviewModel.BaseTotal)] = new()
			{
				DisplayName = "Base Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferItemOverviewModel.DiscountPercent)] = new()
			{
				DisplayName = "Disc %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(StockTransferItemOverviewModel.DiscountAmount)] = new()
			{
				DisplayName = "Disc Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferItemOverviewModel.AfterDiscount)] = new()
			{
				DisplayName = "After Disc",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferItemOverviewModel.SGSTPercent)] = new()
			{
				DisplayName = "SGST %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(StockTransferItemOverviewModel.SGSTAmount)] = new()
			{
				DisplayName = "SGST Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferItemOverviewModel.CGSTPercent)] = new()
			{
				DisplayName = "CGST %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(StockTransferItemOverviewModel.CGSTAmount)] = new()
			{
				DisplayName = "CGST Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferItemOverviewModel.IGSTPercent)] = new()
			{
				DisplayName = "IGST %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(StockTransferItemOverviewModel.IGSTAmount)] = new()
			{
				DisplayName = "IGST Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferItemOverviewModel.TotalTaxAmount)] = new()
			{
				DisplayName = "Tax",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferItemOverviewModel.Total)] = new()
			{
				DisplayName = "Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(StockTransferItemOverviewModel.NetTotal)] = new()
			{
				DisplayName = "Net Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			}
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
				new() { ["Company"] = company?.Name ?? null, ["From Location"] = fromLocation?.Name ?? null, ["To Location"] = toLocation?.Name ?? null }
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
				new() { ["Company"] = company?.Name ?? null, ["From Location"] = fromLocation?.Name ?? null, ["To Location"] = toLocation?.Name ?? null }
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

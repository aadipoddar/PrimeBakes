using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;

namespace PrimeBakesLibrary.Exporting.Restaurant.Bill;

/// <summary>
/// Export utilities for Bill Report and Bill Item Report in PDF and Excel formats.
/// </summary>
public static class BillReportExport
{
	/// <summary>
	/// Exports the bill transaction report to the specified format (PDF or Excel).
	/// </summary>
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<BillOverviewModel> billData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		CompanyModel company = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(BillOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillOverviewModel.DiningTableName)] = new() { DisplayName = "Table", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillOverviewModel.DiningAreaName)] = new() { DisplayName = "Area", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillOverviewModel.CustomerName)] = new() { DisplayName = "Customer", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(BillOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillOverviewModel.Remarks)] = new() { DisplayName = "Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(BillOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(BillOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", Alignment = CellAlignment.Left, IncludeInTotal = false },

			[nameof(BillOverviewModel.TotalPeople)] = new()
			{
				DisplayName = "People",
				Format = "#,##0",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.TotalItems)] = new()
			{
				DisplayName = "Items",
				Format = "#,##0",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.TotalQuantity)] = new()
			{
				DisplayName = "Qty",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.BaseTotal)] = new()
			{
				DisplayName = "Base Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.ItemDiscountAmount)] = new()
			{
				DisplayName = "Item Discount Amount",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.TotalAfterItemDiscount)] = new()
			{
				DisplayName = "After Disc",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillOverviewModel.TotalInclusiveTaxAmount)] = new()
			{
				DisplayName = "Incl Tax",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillOverviewModel.TotalExtraTaxAmount)] = new()
			{
				DisplayName = "Extra Tax",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillOverviewModel.TotalAfterTax)] = new()
			{
				DisplayName = "Sub Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.DiscountPercent)] = new()
			{
				DisplayName = "Disc %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(BillOverviewModel.DiscountAmount)] = new()
			{
				DisplayName = "Disc Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.ServiceChargePercent)] = new()
			{
				DisplayName = "Service %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(BillOverviewModel.ServiceChargeAmount)] = new()
			{
				DisplayName = "Service Charge",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.RoundOffAmount)] = new()
			{
				DisplayName = "Round Off",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.TotalAmount)] = new()
			{
				DisplayName = "Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				IsRequired = true,
				IsGrandTotal = true
			},

			[nameof(BillOverviewModel.Cash)] = new()
			{
				DisplayName = "Cash",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.Card)] = new()
			{
				DisplayName = "Card",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.UPI)] = new()
			{
				DisplayName = "UPI",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.Credit)] = new()
			{
				DisplayName = "Credit",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true
			},

			[nameof(BillOverviewModel.PaymentModes)] = new()
			{
				DisplayName = "Payment Modes",
				Alignment = CellAlignment.Left,
				IncludeInTotal = false
			}
		};

		List<string> columnOrder;

		// Summary view - grouped by location with totals
		if (showSummary)
			columnOrder =
			[
				nameof(BillOverviewModel.LocationName),
				nameof(BillOverviewModel.TotalPeople),
				nameof(BillOverviewModel.TotalItems),
				nameof(BillOverviewModel.TotalQuantity),
				nameof(BillOverviewModel.BaseTotal),
				nameof(BillOverviewModel.ItemDiscountAmount),
				nameof(BillOverviewModel.TotalAfterItemDiscount),
				nameof(BillOverviewModel.TotalInclusiveTaxAmount),
				nameof(BillOverviewModel.TotalExtraTaxAmount),
				nameof(BillOverviewModel.TotalAfterTax),
				nameof(BillOverviewModel.DiscountAmount),
				nameof(BillOverviewModel.ServiceChargeAmount),
				nameof(BillOverviewModel.RoundOffAmount),
				nameof(BillOverviewModel.TotalAmount),
				nameof(BillOverviewModel.Cash),
				nameof(BillOverviewModel.Card),
				nameof(BillOverviewModel.UPI),
				nameof(BillOverviewModel.Credit)
			];

		else if (showAllColumns)
		{
			// All columns - detailed view
			columnOrder =
			[
				nameof(BillOverviewModel.TransactionNo),
				nameof(BillOverviewModel.CompanyName)
			];

			if (location is null)
				columnOrder.Add(nameof(BillOverviewModel.LocationName));

			columnOrder.AddRange(
			[
				nameof(BillOverviewModel.DiningTableName),
				nameof(BillOverviewModel.DiningAreaName),
				nameof(BillOverviewModel.CustomerName),
				nameof(BillOverviewModel.TransactionDateTime),
				nameof(BillOverviewModel.FinancialYear),
				nameof(BillOverviewModel.TotalPeople),
				nameof(BillOverviewModel.TotalItems),
				nameof(BillOverviewModel.TotalQuantity),
				nameof(BillOverviewModel.BaseTotal),
				nameof(BillOverviewModel.ItemDiscountAmount),
				nameof(BillOverviewModel.TotalAfterItemDiscount),
				nameof(BillOverviewModel.TotalInclusiveTaxAmount),
				nameof(BillOverviewModel.TotalExtraTaxAmount),
				nameof(BillOverviewModel.TotalAfterTax),
				nameof(BillOverviewModel.DiscountPercent),
				nameof(BillOverviewModel.DiscountAmount),
				nameof(BillOverviewModel.ServiceChargePercent),
				nameof(BillOverviewModel.ServiceChargeAmount),
				nameof(BillOverviewModel.RoundOffAmount),
				nameof(BillOverviewModel.TotalAmount),
				nameof(BillOverviewModel.Cash),
				nameof(BillOverviewModel.Card),
				nameof(BillOverviewModel.UPI),
				nameof(BillOverviewModel.Credit),
				nameof(BillOverviewModel.PaymentModes),
				nameof(BillOverviewModel.Remarks),
				nameof(BillOverviewModel.CreatedByName),
				nameof(BillOverviewModel.CreatedAt),
				nameof(BillOverviewModel.CreatedFromPlatform),
				nameof(BillOverviewModel.LastModifiedByUserName),
				nameof(BillOverviewModel.LastModifiedAt),
				nameof(BillOverviewModel.LastModifiedFromPlatform)
			]);
		}
		else
		{
			// Summary columns - key fields only
			columnOrder =
			[
				nameof(BillOverviewModel.TransactionNo),
				nameof(BillOverviewModel.DiningTableName),
				nameof(BillOverviewModel.TransactionDateTime),
				nameof(BillOverviewModel.TotalQuantity),
				nameof(BillOverviewModel.TotalAfterTax),
				nameof(BillOverviewModel.DiscountPercent),
				nameof(BillOverviewModel.DiscountAmount),
				nameof(BillOverviewModel.ServiceChargeAmount),
				nameof(BillOverviewModel.TotalAmount),
				nameof(BillOverviewModel.PaymentModes)
			];

			if (location is null)
				columnOrder.Insert(2, nameof(BillOverviewModel.LocationName));
		}

		if (company is not null)
			columnOrder.Remove(nameof(BillOverviewModel.CompanyName));

		if (location is not null)
			columnOrder.Remove(nameof(BillOverviewModel.LocationName));

		string fileName = "BILL_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				billData,
				"BILL REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null }
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				billData,
				"BILL REPORT",
				"Bill Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null }
			);

			return (stream, fileName + ".xlsx");
		}
	}

	/// <summary>
	/// Exports the bill item report to the specified format (PDF or Excel).
	/// </summary>
	public static async Task<(MemoryStream stream, string fileName)> ExportItemReport(
		IEnumerable<BillItemOverviewModel> billItemData,
		ReportExportType exportType,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		CompanyModel company = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, ReportColumnSetting>
		{
			[nameof(BillItemOverviewModel.ItemName)] = new() { DisplayName = "Item", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", Alignment = CellAlignment.Center, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.LocationName)] = new() { DisplayName = "Location", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.DiningTableName)] = new() { DisplayName = "Table", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.DiningAreaName)] = new() { DisplayName = "Area", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.CustomerName)] = new() { DisplayName = "Customer", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.BillRemarks)] = new() { DisplayName = "Bill Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", Alignment = CellAlignment.Left, IncludeInTotal = false },
			[nameof(BillItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", Alignment = CellAlignment.Center, IncludeInTotal = false },

			[nameof(BillItemOverviewModel.Quantity)] = new()
			{
				DisplayName = "Qty",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillItemOverviewModel.Rate)] = new()
			{
				DisplayName = "Rate",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = false
			},

			[nameof(BillItemOverviewModel.NetRate)] = new()
			{
				DisplayName = "Net Rate",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = false
			},

			[nameof(BillItemOverviewModel.BaseTotal)] = new()
			{
				DisplayName = "Base Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillItemOverviewModel.DiscountPercent)] = new()
			{
				DisplayName = "Disc %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(BillItemOverviewModel.DiscountAmount)] = new()
			{
				DisplayName = "Discount Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillItemOverviewModel.AfterDiscount)] = new()
			{
				DisplayName = "After Disc",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillItemOverviewModel.SGSTPercent)] = new()
			{
				DisplayName = "SGST %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(BillItemOverviewModel.SGSTAmount)] = new()
			{
				DisplayName = "SGST Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillItemOverviewModel.CGSTPercent)] = new()
			{
				DisplayName = "CGST %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(BillItemOverviewModel.CGSTAmount)] = new()
			{
				DisplayName = "CGST Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillItemOverviewModel.IGSTPercent)] = new()
			{
				DisplayName = "IGST %",
				Format = "#,##0.00",
				Alignment = CellAlignment.Center,
				IncludeInTotal = false
			},

			[nameof(BillItemOverviewModel.IGSTAmount)] = new()
			{
				DisplayName = "IGST Amt",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillItemOverviewModel.TotalTaxAmount)] = new()
			{
				DisplayName = "Tax",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillItemOverviewModel.Total)] = new()
			{
				DisplayName = "Total",
				Format = "#,##0.00",
				Alignment = CellAlignment.Right,
				IncludeInTotal = true,
				HighlightNegative = true
			},

			[nameof(BillItemOverviewModel.NetTotal)] = new()
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
				nameof(BillItemOverviewModel.ItemName),
				nameof(BillItemOverviewModel.ItemCode),
				nameof(BillItemOverviewModel.ItemCategoryName),
				nameof(BillItemOverviewModel.Quantity),
				nameof(BillItemOverviewModel.BaseTotal),
				nameof(BillItemOverviewModel.DiscountAmount),
				nameof(BillItemOverviewModel.AfterDiscount),
				nameof(BillItemOverviewModel.SGSTAmount),
				nameof(BillItemOverviewModel.CGSTAmount),
				nameof(BillItemOverviewModel.IGSTAmount),
				nameof(BillItemOverviewModel.TotalTaxAmount),
				nameof(BillItemOverviewModel.Total),
				nameof(BillItemOverviewModel.NetTotal)
			];

		else if (showAllColumns)
		{
			List<string> columns =
			[
				nameof(BillItemOverviewModel.ItemName),
				nameof(BillItemOverviewModel.ItemCode),
				nameof(BillItemOverviewModel.ItemCategoryName),
				nameof(BillItemOverviewModel.TransactionNo),
				nameof(BillItemOverviewModel.TransactionDateTime),
				nameof(BillItemOverviewModel.CompanyName)
			];

			if (location is null)
				columns.Add(nameof(BillItemOverviewModel.LocationName));

			columns.AddRange([
				nameof(BillItemOverviewModel.DiningTableName),
				nameof(BillItemOverviewModel.DiningAreaName),
				nameof(BillItemOverviewModel.CustomerName),
				nameof(BillItemOverviewModel.Quantity),
				nameof(BillItemOverviewModel.Rate),
				nameof(BillItemOverviewModel.BaseTotal),
				nameof(BillItemOverviewModel.DiscountPercent),
				nameof(BillItemOverviewModel.DiscountAmount),
				nameof(BillItemOverviewModel.AfterDiscount),
				nameof(BillItemOverviewModel.SGSTPercent),
				nameof(BillItemOverviewModel.SGSTAmount),
				nameof(BillItemOverviewModel.CGSTPercent),
				nameof(BillItemOverviewModel.CGSTAmount),
				nameof(BillItemOverviewModel.IGSTPercent),
				nameof(BillItemOverviewModel.IGSTAmount),
				nameof(BillItemOverviewModel.TotalTaxAmount),
				nameof(BillItemOverviewModel.InclusiveTax),
				nameof(BillItemOverviewModel.Total),
				nameof(BillItemOverviewModel.NetRate),
				nameof(BillItemOverviewModel.NetTotal),
				nameof(BillItemOverviewModel.BillRemarks),
				nameof(BillItemOverviewModel.Remarks)
			]);

			columnOrder = columns;
		}
		else
			columnOrder =
			[
				nameof(BillItemOverviewModel.ItemName),
				nameof(BillItemOverviewModel.ItemCode),
				nameof(BillItemOverviewModel.TransactionNo),
				nameof(BillItemOverviewModel.TransactionDateTime),
				nameof(BillItemOverviewModel.LocationName),
				nameof(BillItemOverviewModel.DiningTableName),
				nameof(BillItemOverviewModel.Quantity),
				nameof(BillItemOverviewModel.NetRate),
				nameof(BillItemOverviewModel.NetTotal)
			];

		string fileName = "BILL_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";

		if (exportType == ReportExportType.PDF)
		{
			var stream = await PDFReportExportUtil.ExportToPdf(
				billItemData,
				"BILL ITEM REPORT",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				useBuiltInStyle: false,
				useLandscape: showAllColumns || showSummary,
				new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null }
			);

			return (stream, fileName + ".pdf");
		}
		else
		{
			var stream = await ExcelReportExportUtil.ExportToExcel(
				billItemData,
				"BILL ITEM REPORT",
				"Bill Item Transactions",
				dateRangeStart,
				dateRangeEnd,
				columnSettings,
				columnOrder,
				new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null }
			);

			return (stream, fileName + ".xlsx");
		}
	}
}

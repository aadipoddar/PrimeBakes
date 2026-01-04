using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

public static class SaleItemReportPDFExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<SaleItemOverviewModel> saleItemData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		LedgerModel party = null,
		CompanyModel company = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
		{
			[nameof(SaleItemOverviewModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.LocationName)] = new() { DisplayName = "Location", IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.SaleRemarks)] = new() { DisplayName = "Sale Remarks", IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", IncludeInTotal = false },
			[nameof(SaleItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", IncludeInTotal = false },

			[nameof(SaleItemOverviewModel.Quantity)] = new()
			{
				DisplayName = "Qty",
				Format = "#,##0.00",
				HighlightNegative = true,
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(SaleItemOverviewModel.Rate)] = new()
			{
				DisplayName = "Rate",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.NetRate)] = new()
			{
				DisplayName = "Net Rate",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.BaseTotal)] = new()
			{
				DisplayName = "Base Total",
				Format = "#,##0.00",
				HighlightNegative = true,
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(SaleItemOverviewModel.DiscountPercent)] = new()
			{
				DisplayName = "Disc %",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.DiscountAmount)] = new()
			{
				DisplayName = "Disc Amt",
				Format = "#,##0.00",
				HighlightNegative = true,
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(SaleItemOverviewModel.AfterDiscount)] = new()
			{
				DisplayName = "After Disc",
				Format = "#,##0.00",
				HighlightNegative = true,
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(SaleItemOverviewModel.SGSTPercent)] = new()
			{
				DisplayName = "SGST %",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.SGSTAmount)] = new()
			{
				DisplayName = "SGST Amt",
				Format = "#,##0.00",
				HighlightNegative = true,
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(SaleItemOverviewModel.CGSTPercent)] = new()
			{
				DisplayName = "CGST %",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.CGSTAmount)] = new()
			{
				DisplayName = "CGST Amt",
				Format = "#,##0.00",
				HighlightNegative = true,
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(SaleItemOverviewModel.IGSTPercent)] = new()
			{
				DisplayName = "IGST %",
				Format = "#,##0.00",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Center,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				},
				IncludeInTotal = false
			},

			[nameof(SaleItemOverviewModel.IGSTAmount)] = new()
			{
				DisplayName = "IGST Amt",
				Format = "#,##0.00",
				HighlightNegative = true,
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(SaleItemOverviewModel.TotalTaxAmount)] = new()
			{
				DisplayName = "Tax",
				Format = "#,##0.00",
				HighlightNegative = true,
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(SaleItemOverviewModel.Total)] = new()
			{
				DisplayName = "Total",
				Format = "#,##0.00",
				HighlightNegative = true,
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(SaleItemOverviewModel.NetTotal)] = new()
			{
				DisplayName = "Net Total",
				Format = "#,##0.00",
				HighlightNegative = true,
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
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
			// All columns - detailed view (matching Excel export)
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
		// Summary columns - key fields only (matching Excel export)
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

		string fileName = "SALE_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
		fileName += ".pdf";

		return (stream, fileName);
	}
}

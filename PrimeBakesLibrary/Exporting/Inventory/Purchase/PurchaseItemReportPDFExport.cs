using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

public static class PurchaseItemReportPDFExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<PurchaseItemOverviewModel> purchaseItemData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		LedgerModel party = null,
		CompanyModel company = null)
	{
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
		{
			[nameof(PurchaseItemOverviewModel.ItemName)] = new() { DisplayName = "Item", IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.ItemCode)] = new() { DisplayName = "Code", IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.ItemCategoryName)] = new() { DisplayName = "Category", IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.PartyName)] = new() { DisplayName = "Party", IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.PurchaseRemarks)] = new() { DisplayName = "Purchase Remarks", IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.Remarks)] = new() { DisplayName = "Item Remarks", IncludeInTotal = false },
			[nameof(PurchaseItemOverviewModel.InclusiveTax)] = new() { DisplayName = "Incl Tax", IncludeInTotal = false },

			[nameof(PurchaseItemOverviewModel.Quantity)] = new()
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

			[nameof(PurchaseItemOverviewModel.Rate)] = new()
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

			[nameof(PurchaseItemOverviewModel.NetRate)] = new()
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

			[nameof(PurchaseItemOverviewModel.BaseTotal)] = new()
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

			[nameof(PurchaseItemOverviewModel.DiscountPercent)] = new()
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

			[nameof(PurchaseItemOverviewModel.DiscountAmount)] = new()
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

			[nameof(PurchaseItemOverviewModel.AfterDiscount)] = new()
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

			[nameof(PurchaseItemOverviewModel.SGSTPercent)] = new()
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

			[nameof(PurchaseItemOverviewModel.SGSTAmount)] = new()
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

			[nameof(PurchaseItemOverviewModel.CGSTPercent)] = new()
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

			[nameof(PurchaseItemOverviewModel.CGSTAmount)] = new()
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

			[nameof(PurchaseItemOverviewModel.IGSTPercent)] = new()
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

			[nameof(PurchaseItemOverviewModel.IGSTAmount)] = new()
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

			[nameof(PurchaseItemOverviewModel.TotalTaxAmount)] = new()
			{
				DisplayName = "Tax Amt",
				Format = "#,##0.00",
				HighlightNegative = true,
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},

			[nameof(PurchaseItemOverviewModel.Total)] = new()
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

			[nameof(PurchaseItemOverviewModel.NetTotal)] = new()
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
		{
			columnOrder =
			[
				nameof(PurchaseItemOverviewModel.ItemName),
				nameof(PurchaseItemOverviewModel.ItemCode),
				nameof(PurchaseItemOverviewModel.ItemCategoryName),
				nameof(PurchaseItemOverviewModel.Quantity),
				nameof(PurchaseItemOverviewModel.BaseTotal),
				nameof(PurchaseItemOverviewModel.DiscountAmount),
				nameof(PurchaseItemOverviewModel.AfterDiscount),
				nameof(PurchaseItemOverviewModel.SGSTAmount),
				nameof(PurchaseItemOverviewModel.CGSTAmount),
				nameof(PurchaseItemOverviewModel.IGSTAmount),
				nameof(PurchaseItemOverviewModel.TotalTaxAmount),
				nameof(PurchaseItemOverviewModel.Total),
				nameof(PurchaseItemOverviewModel.NetTotal)
			];
		}

		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(PurchaseItemOverviewModel.ItemName),
				nameof(PurchaseItemOverviewModel.ItemCode),
				nameof(PurchaseItemOverviewModel.ItemCategoryName),
				nameof(PurchaseItemOverviewModel.TransactionNo),
				nameof(PurchaseItemOverviewModel.TransactionDateTime),
				nameof(PurchaseItemOverviewModel.CompanyName),
				nameof(PurchaseItemOverviewModel.PartyName),
				nameof(PurchaseItemOverviewModel.Quantity),
				nameof(PurchaseItemOverviewModel.Rate),
				nameof(PurchaseItemOverviewModel.BaseTotal),
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
				nameof(PurchaseItemOverviewModel.PurchaseRemarks),
				nameof(PurchaseItemOverviewModel.Remarks)
			];

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseItemOverviewModel.PartyName));

			if (company is not null)
				columnOrder.Remove(nameof(PurchaseItemOverviewModel.CompanyName));
		}

		else
		{
			columnOrder =
			[
				nameof(PurchaseItemOverviewModel.ItemName),
				nameof(PurchaseItemOverviewModel.ItemCode),
				nameof(PurchaseItemOverviewModel.TransactionNo),
				nameof(PurchaseItemOverviewModel.TransactionDateTime),
				nameof(PurchaseItemOverviewModel.PartyName),
				nameof(PurchaseItemOverviewModel.Quantity),
				nameof(PurchaseItemOverviewModel.NetRate),
				nameof(PurchaseItemOverviewModel.NetTotal)
			];

			if (party is not null)
				columnOrder.Remove(nameof(PurchaseItemOverviewModel.PartyName));
		}

		var stream = await PDFReportExportUtil.ExportToPdf(
			purchaseItemData,
			"PURCHASE ITEM REPORT",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			useBuiltInStyle: false,
			useLandscape: showAllColumns || showSummary,
			new() { ["Company"] = company?.Name ?? null, ["Party"] = party?.Name ?? null }
		);

		string fileName = $"PURCHASE_ITEM_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
		fileName += ".pdf";

		return (stream, fileName);
	}
}

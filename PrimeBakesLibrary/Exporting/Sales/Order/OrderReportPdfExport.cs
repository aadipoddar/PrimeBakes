using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Order;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

public static class OrderReportPdfExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportReport(
		IEnumerable<OrderOverviewModel> orderData,
		DateOnly? dateRangeStart = null,
		DateOnly? dateRangeEnd = null,
		bool showAllColumns = true,
		bool showSummary = false,
		CompanyModel company = null,
		LocationModel location = null)
	{
		var columnSettings = new Dictionary<string, PDFReportExportUtil.ColumnSetting>
		{
			[nameof(OrderOverviewModel.TransactionNo)] = new() { DisplayName = "Trans No", IncludeInTotal = false },
			[nameof(OrderOverviewModel.SaleTransactionNo)] = new() { DisplayName = "Sale Trans No", IncludeInTotal = false },
			[nameof(OrderOverviewModel.CompanyName)] = new() { DisplayName = "Company", IncludeInTotal = false },
			[nameof(OrderOverviewModel.LocationName)] = new() { DisplayName = "Location", IncludeInTotal = false },
			[nameof(OrderOverviewModel.TransactionDateTime)] = new() { DisplayName = "Trans Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false },
			[nameof(OrderOverviewModel.SaleDateTime)] = new() { DisplayName = "Sale Date", Format = "dd-MMM-yyyy hh:mm tt", IncludeInTotal = false },
			[nameof(OrderOverviewModel.FinancialYear)] = new() { DisplayName = "Financial Year", IncludeInTotal = false },
			[nameof(OrderOverviewModel.Remarks)] = new() { DisplayName = "Remarks", IncludeInTotal = false },
			[nameof(OrderOverviewModel.CreatedByName)] = new() { DisplayName = "Created By", IncludeInTotal = false },
			[nameof(OrderOverviewModel.CreatedAt)] = new() { DisplayName = "Created At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
			[nameof(OrderOverviewModel.CreatedFromPlatform)] = new() { DisplayName = "Created Platform", IncludeInTotal = false },
			[nameof(OrderOverviewModel.LastModifiedByUserName)] = new() { DisplayName = "Modified By", IncludeInTotal = false },
			[nameof(OrderOverviewModel.LastModifiedAt)] = new() { DisplayName = "Modified At", Format = "dd-MMM-yyyy hh:mm", IncludeInTotal = false },
			[nameof(OrderOverviewModel.LastModifiedFromPlatform)] = new() { DisplayName = "Modified Platform", IncludeInTotal = false },
			[nameof(OrderOverviewModel.TotalItems)] = new()
			{
				DisplayName = "Items",
				Format = "#,##0",
				StringFormat = new Syncfusion.Pdf.Graphics.PdfStringFormat
				{
					Alignment = Syncfusion.Pdf.Graphics.PdfTextAlignment.Right,
					LineAlignment = Syncfusion.Pdf.Graphics.PdfVerticalAlignment.Middle
				}
			},
			[nameof(OrderOverviewModel.TotalQuantity)] = new()
			{
				DisplayName = "Qty",
				Format = "#,##0.00",
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
				nameof(OrderOverviewModel.LocationName),
				nameof(OrderOverviewModel.TotalItems),
				nameof(OrderOverviewModel.TotalQuantity)
			];
		else if (showAllColumns)
		{
			columnOrder =
			[
				nameof(OrderOverviewModel.TransactionNo),
				nameof(OrderOverviewModel.SaleTransactionNo),
				nameof(OrderOverviewModel.CompanyName),
				nameof(OrderOverviewModel.LocationName),
				nameof(OrderOverviewModel.TransactionDateTime),
				nameof(OrderOverviewModel.SaleDateTime),
				nameof(OrderOverviewModel.FinancialYear),
				nameof(OrderOverviewModel.TotalItems),
				nameof(OrderOverviewModel.TotalQuantity),
				nameof(OrderOverviewModel.Remarks),
				nameof(OrderOverviewModel.CreatedByName),
				nameof(OrderOverviewModel.CreatedAt),
				nameof(OrderOverviewModel.CreatedFromPlatform),
				nameof(OrderOverviewModel.LastModifiedByUserName),
				nameof(OrderOverviewModel.LastModifiedAt),
				nameof(OrderOverviewModel.LastModifiedFromPlatform)
			];
		}
		else
		{
			columnOrder =
			[
				nameof(OrderOverviewModel.TransactionNo),
				nameof(OrderOverviewModel.SaleTransactionNo),
				nameof(OrderOverviewModel.TransactionDateTime),
				nameof(OrderOverviewModel.TotalItems),
				nameof(OrderOverviewModel.TotalQuantity)
			];
		}

		if (company is not null)
			columnOrder.Remove(nameof(OrderOverviewModel.CompanyName));

		if (location is not null)
			columnOrder.Remove(nameof(OrderOverviewModel.LocationName));

		var stream = await PDFReportExportUtil.ExportToPdf(
			orderData,
			"ORDER REPORT",
			dateRangeStart,
			dateRangeEnd,
			columnSettings,
			columnOrder,
			useBuiltInStyle: false,
			useLandscape: showAllColumns && !showSummary,
			new() { ["Company"] = company?.Name ?? null, ["Location"] = location?.Name ?? null }
		);

		string fileName = "ORDER_REPORT";
		if (dateRangeStart.HasValue || dateRangeEnd.HasValue)
			fileName += $"_{dateRangeStart?.ToString("yyyyMMdd") ?? "START"}_to_{dateRangeEnd?.ToString("yyyyMMdd") ?? "END"}";
		fileName += ".pdf";

		return (stream, fileName);
	}
}

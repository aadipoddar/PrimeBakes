using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.StockTransfer;

namespace PrimeBakes.Exports.Store.StockTransfer;

public static class StockTransferInvoiceExport
{
	public static (MemoryStream stream, string fileName) ExportInvoice(StockTransferInvoiceBundle bundle, InvoiceExportType exportType)
	{
		var (transaction, transactionDetails, company, toLocationLedger, currentDateTime) = bundle;
		var lineItems = transactionDetails.Select(detail => new
		{
			detail.ItemName,
			detail.Quantity,
			detail.Rate,
			detail.DiscountPercent,
			AfterDiscount = detail.DiscountPercent == 0 ? 0 : detail.AfterDiscount,
			CGSTPercent = detail.InclusiveTax ? 0 : detail.CGSTPercent,
			SGSTPercent = detail.InclusiveTax ? 0 : detail.SGSTPercent,
			IGSTPercent = detail.InclusiveTax ? 0 : detail.IGSTPercent,
			TaxPercent = detail.InclusiveTax ? 0 : detail.CGSTPercent + detail.SGSTPercent + detail.IGSTPercent,
			TotalTaxAmount = detail.InclusiveTax ? 0 : detail.TotalTaxAmount,
			detail.Total
		}).ToList();

		var paymentModes = new Dictionary<string, decimal>();
		if (transaction.Cash > 0) paymentModes.Add("Cash", transaction.Cash);
		if (transaction.Card > 0) paymentModes.Add("Card", transaction.Card);
		if (transaction.UPI > 0) paymentModes.Add("UPI", transaction.UPI);
		if (transaction.Credit > 0) paymentModes.Add("Credit", transaction.Credit);

		var invoiceData = new InvoiceData
		{
			CurrentDateTime = currentDateTime,
			Company = company,
			BillTo = toLocationLedger,
			InvoiceType = "STOCK TRANSFER INVOICE",
			Outlet = transaction.LocationName,
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			ReferenceTransactionNo = string.Empty,
			ReferenceDateTime = null,
			TotalAmount = transaction.TotalAmount,
			Remarks = transaction.Remarks ?? string.Empty,
			Status = transaction.Status,
			PaymentModes = paymentModes
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Sub Total"] = transaction.TotalAfterTax.FormatIndianCurrency(),
			[$"Other Charges ({transaction.OtherChargesPercent:0.00}%)"] = transaction.OtherChargesAmount.FormatIndianCurrency(),
			[$"Discount ({transaction.DiscountPercent:0.00}%)"] = transaction.DiscountAmount.FormatIndianCurrency(),
			["Round Off"] = transaction.RoundOffAmount.FormatIndianCurrency(),
			["Grand Total"] = transaction.TotalAmount.FormatIndianCurrency()
		};

		var columnSettings = new List<InvoiceColumnSetting>
		{
			new("#", "#", exportType, CellAlignment.Center, 25, 5),
			new(nameof(StockTransferItemOverviewModel.ItemName), "Item", exportType, CellAlignment.Left, 0, 30),
			new(nameof(StockTransferItemOverviewModel.Quantity), "Qty", exportType, CellAlignment.Right, 40, 10, "#,##0.00"),
			new(nameof(StockTransferItemOverviewModel.Rate), "Rate", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
			new(nameof(StockTransferItemOverviewModel.DiscountPercent), "Disc %", exportType, CellAlignment.Right, 45, 8, "#,##0.00"),
			new(nameof(StockTransferItemOverviewModel.AfterDiscount), "Taxable", exportType, CellAlignment.Right, 55, 12, "#,##0.00"),
			new("TaxPercent","Tax %", exportType, CellAlignment.Right, 45, 8, "#,##0.00"),
			new(nameof(StockTransferItemOverviewModel.TotalTaxAmount), "Tax", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
			new(nameof(StockTransferItemOverviewModel.Total), "Total", exportType, CellAlignment.Right, 55, 15, "#,##0.00")
		};

		string fileName = $"STOCK_TRANSFER_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == InvoiceExportType.PDF)
		{
			var stream = PDFInvoiceExportUtil.ExportInvoiceToPdf(
				invoiceData,
				lineItems,
				columnSettings,
				null,
				summaryFields
			);

			fileName += ".pdf";
			return (stream, fileName);
		}
		else
		{
			var stream = ExcelInvoiceExportUtil.ExportInvoiceToExcel(
				invoiceData,
				lineItems,
				columnSettings,
				null,
				summaryFields
			);

			fileName += ".xlsx";
			return (stream, fileName);
		}
	}
}

using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Order;

namespace PrimeBakes.Exports.Store.Order;

public static class OrderInvoiceExport
{
	public static (MemoryStream stream, string fileName) ExportInvoice(OrderInvoiceBundle bundle, InvoiceExportType exportType)
	{
		var (transaction, transactionDetails, company, locationLedger, currentDateTime) = bundle;
		var lineItems = transactionDetails.Select(detail => new
		{
			detail.ItemName,
			detail.Quantity
		}).ToList();

		var invoiceData = new InvoiceData
		{
			CurrentDateTime = currentDateTime,
			Company = company,
			BillTo = locationLedger,
			InvoiceType = "ORDER INVOICE",
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			ReferenceTransactionNo = transaction.SaleTransactionNo ?? string.Empty,
			ReferenceDateTime = transaction.SaleDateTime,
			TotalAmount = 0, // Orders don't have amounts
			Remarks = transaction.Remarks ?? string.Empty,
			Status = transaction.Status,
			PaymentModes = null
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Total Items"] = transaction.TotalItems.ToString(),
			["Total Quantity"] = transaction.TotalQuantity.ToString("#,##0.00")
		};

		var columnSettings = new List<InvoiceColumnSetting>
		{
			new("#", "#", exportType, CellAlignment.Center, 25, 5),
			new(nameof(OrderItemOverviewModel.ItemName), "Item", exportType, CellAlignment.Left, 0, 50),
			new(nameof(OrderItemOverviewModel.Quantity), "Qty", exportType, CellAlignment.Right, 60, 15, "#,##0.00")
		};

		string fileName = $"ORDER_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

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

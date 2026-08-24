using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.PurchaseOrder;

namespace PrimeBakes.Exports.Inventory.PurchaseOrder;

public static class PurchaseOrderInvoiceExport
{
	public static (MemoryStream stream, string fileName) ExportInvoice(PurchaseOrderInvoiceBundle bundle, InvoiceExportType exportType)
	{
		var (transaction, transactionDetails, company, party, currentDateTime) = bundle;
		if (transaction.ExpectedDeliveryDate is not null)
			party.Address += $"\nExpected Delivery: {transaction.ExpectedDeliveryDate:dd-MMM-yyyy}";

		var lineItems = transactionDetails.Select(detail => new
		{
			detail.ItemName,
			detail.ItemCode,
			detail.Quantity,
			detail.UnitOfMeasurement
		}).ToList();

		var invoiceData = new InvoiceData
		{
			CurrentDateTime = currentDateTime,
			Company = company,
			BillTo = party,
			InvoiceType = "PURCHASE ORDER",
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			ReferenceTransactionNo = transaction.PurchaseTransactionNo ?? string.Empty,
			ReferenceDateTime = transaction.PurchaseDateTime,
			TotalAmount = 0, // Purchase orders carry quantities only
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
			new(nameof(PurchaseOrderItemOverviewModel.ItemName), "Item", exportType, CellAlignment.Left, 0, 40),
			new(nameof(PurchaseOrderItemOverviewModel.ItemCode), "Code", exportType, CellAlignment.Left, 70, 15),
			new(nameof(PurchaseOrderItemOverviewModel.Quantity), "Qty", exportType, CellAlignment.Right, 60, 15, "#,##0.00"),
			new(nameof(PurchaseOrderItemOverviewModel.UnitOfMeasurement), "Unit", exportType, CellAlignment.Center, 50, 10)
		};

		string fileName = $"PURCHASE_ORDER_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

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

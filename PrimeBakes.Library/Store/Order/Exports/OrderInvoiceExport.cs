using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Common;
using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Store.Order.Models;
using PrimeBakes.Library.Utils.Exports;

namespace PrimeBakes.Library.Store.Order.Exports;

public static class OrderInvoiceExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType)
	{
		var transaction = await CommonData.LoadTableDataById<OrderOverviewModel>(StoreNames.OrderOverview, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<OrderItemOverviewModel>(StoreNames.OrderItemOverview, transaction.Id);
		transactionDetails = [.. transactionDetails.OrderBy(detail => detail.ItemName)];
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, transaction.CompanyId);
		var locationLedger = await LocationData.LoadLedgerByLocationId(transaction.LocationId);
		if (company is null || locationLedger is null)
			throw new InvalidOperationException("Company or location information is missing.");

		var lineItems = transactionDetails.Select(detail => new
		{
			detail.ItemName,
			detail.Quantity
		}).ToList();

		var invoiceData = new InvoiceData
		{
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

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		string fileName = $"ORDER_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

		if (exportType == InvoiceExportType.PDF)
		{
			var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
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
			var stream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
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

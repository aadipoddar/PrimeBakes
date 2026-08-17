using PrimeBakes.Exports.Utils.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;

namespace PrimeBakes.Exports.Inventory.Kitchen;

public static class KitchenProductionInvoiceExport
{
	public static (MemoryStream stream, string fileName) ExportInvoice(KitchenProductionInvoiceBundle bundle, InvoiceExportType exportType)
	{
		var (transaction, transactionDetails, company, kitchen, currentDateTime) = bundle;
		var lineItems = transactionDetails.Select(detail => new
		{
			detail.ItemName,
			detail.Quantity,
			detail.Rate,
			detail.Total
		}).ToList();

		// Convert LocationModel to LedgerModel for display
		var kitchenAsLedger = new LedgerModel
		{
			Name = kitchen.Name,
		};

		var invoiceData = new InvoiceData
		{
			CurrentDateTime = currentDateTime,
			Company = company,
			BillTo = kitchenAsLedger,
			InvoiceType = "KITCHEN PRODUCTION INVOICE",
			Outlet = kitchen?.Name ?? string.Empty,
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			TotalAmount = transaction.TotalAmount,
			Remarks = transaction.Remarks ?? string.Empty,
			Status = transaction.Status,
			PaymentModes = null
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Grand Total"] = transaction.TotalAmount.FormatIndianCurrency()
		};

		var columnSettings = new List<InvoiceColumnSetting>
		{
			new("#", "#", exportType, CellAlignment.Center, 25, 5),
			new(nameof(KitchenProductionItemOverviewModel.ItemName), "Item", exportType, CellAlignment.Left, 0, 40),
			new(nameof(KitchenProductionItemOverviewModel.Quantity), "Qty", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
			new(nameof(KitchenProductionItemOverviewModel.Rate), "Rate", exportType, CellAlignment.Right, 60, 12, "#,##0.00"),
			new(nameof(KitchenProductionItemOverviewModel.Total), "Total", exportType, CellAlignment.Right, 60, 15, "#,##0.00")
		};

		string fileName = $"KITCHEN_PRODUCTION_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

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

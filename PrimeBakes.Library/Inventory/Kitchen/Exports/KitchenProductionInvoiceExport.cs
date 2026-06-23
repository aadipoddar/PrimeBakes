using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Common;
using PrimeBakes.Library.Inventory.Kitchen.Models;
using PrimeBakes.Library.Utils.Exports;

namespace PrimeBakes.Library.Inventory.Kitchen.Exports;

public static class KitchenProductionInvoiceExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType)
	{
		var transaction = await CommonData.LoadTableDataById<KitchenProductionOverviewModel>(InventoryNames.KitchenProductionOverview, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionItemOverviewModel>(InventoryNames.KitchenProductionItemOverview, transaction.Id);
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, transaction.CompanyId);
		var kitchen = await CommonData.LoadTableDataById<KitchenModel>(InventoryNames.Kitchen, transaction.KitchenId);
		if (company is null || kitchen is null)
			throw new InvalidOperationException("Company or kitchen information is missing.");

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

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		string fileName = $"KITCHEN_PRODUCTION_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

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

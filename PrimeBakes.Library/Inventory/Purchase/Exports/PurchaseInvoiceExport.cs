using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Common;
using PrimeBakes.Library.Inventory.Purchase.Models;
using PrimeBakes.Library.Utils.Exports;

namespace PrimeBakes.Library.Inventory.Purchase.Exports;

public static class PurchaseInvoiceExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType)
	{
		var transaction = await CommonData.LoadTableDataById<PurchaseOverviewModel>(InventoryNames.PurchaseOverview, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<PurchaseItemOverviewModel>(InventoryNames.PurchaseItemOverview, transaction.Id);
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, transaction.CompanyId);
		var party = await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, transaction.PartyId);
		if (company is null || party is null)
			throw new InvalidOperationException("Company or party information is missing.");

		if (!string.IsNullOrWhiteSpace(transaction.ChallanNo))
			party.Address += $"\nChallan: {transaction.ChallanNo}";

		var lineItems = transactionDetails.Select(detail => new
		{
			detail.ItemName,
			detail.Quantity,
			detail.UnitOfMeasurement,
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

		var invoiceData = new InvoiceData
		{
			Company = company,
			BillTo = party,
			InvoiceType = "PURCHASE INVOICE",
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			TotalAmount = transaction.TotalAmount,
			Remarks = transaction.Remarks ?? string.Empty,
			Status = transaction.Status,
			PaymentModes = null
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Items Total"] = transaction.TotalAfterTax.FormatIndianCurrency(),
			[$"Other Charges ({transaction.OtherChargesPercent:0.00}%)"] = transaction.OtherChargesAmount.FormatIndianCurrency(),
			[$"Cash Discount ({transaction.CashDiscountPercent:0.00}%)"] = transaction.CashDiscountAmount.FormatIndianCurrency(),
			["Round Off"] = transaction.RoundOffAmount.FormatIndianCurrency(),
			["Grand Total"] = transaction.TotalAmount.FormatIndianCurrency()
		};

		var columnSettings = new List<InvoiceColumnSetting>
		{
			new("#", "#", exportType, CellAlignment.Center, 25, 5),
			new(nameof(PurchaseItemOverviewModel.ItemName), "Item", exportType, CellAlignment.Left, 0, 30),
			new(nameof(PurchaseItemOverviewModel.UnitOfMeasurement), "UOM", exportType, CellAlignment.Center, 40, 8),
			new(nameof(PurchaseItemOverviewModel.Quantity), "Qty", exportType, CellAlignment.Right, 40, 10, "#,##0.00"),
			new(nameof(PurchaseItemOverviewModel.Rate), "Rate", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
			new(nameof(PurchaseItemOverviewModel.DiscountPercent), "Disc %", exportType, CellAlignment.Right, 45, 8, "#,##0.00"),
			new(nameof(PurchaseItemOverviewModel.AfterDiscount), "Taxable", exportType, CellAlignment.Right, 55, 12, "#,##0.00"),
			new("TaxPercent","Tax %", exportType, CellAlignment.Right, 45, 8, "#,##0.00"),
			new(nameof(PurchaseItemOverviewModel.TotalTaxAmount), "Tax", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
			new(nameof(PurchaseItemOverviewModel.Total), "Total", exportType, CellAlignment.Right, 55, 15, "#,##0.00")
		};

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		string fileName = $"PURCHASE_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

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

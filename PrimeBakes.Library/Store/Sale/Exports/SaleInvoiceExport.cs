using PrimeBakes.Library.Accounts.Masters.Models;
using PrimeBakes.Library.Common;
using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Library.Store.Customer.Models;
using PrimeBakes.Library.Store.Order.Models;
using PrimeBakes.Library.Store.Sale.Models;
using PrimeBakes.Library.Utils.Exports;

namespace PrimeBakes.Library.Store.Sale.Exports;

public static class SaleInvoiceExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType)
	{
		var transaction = await CommonData.LoadTableDataById<SaleOverviewModel>(StoreNames.SaleOverview, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleItemOverviewModel>(StoreNames.SaleItemOverview, transaction.Id);
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, transaction.CompanyId);
		var location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, transaction.LocationId);

		LedgerModel? party = null;

		if (transaction.PartyId.HasValue)
			party = await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, transaction.PartyId.Value);
		else if (transaction.CustomerId.HasValue)
		{
			// If no party, convert customer to ledger
			var customer = await CommonData.LoadTableDataById<CustomerModel>(StoreNames.Customer, transaction.CustomerId.Value);
			if (customer is not null)
				party = new LedgerModel
				{
					Name = customer.Name,
					Phone = customer.Number,
				};
		}

		OrderModel? order = null;
		if (transaction.OrderId.HasValue)
			order = await CommonData.LoadTableDataById<OrderModel>(StoreNames.Order, transaction.OrderId.Value);

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
			Company = company,
			BillTo = party,
			InvoiceType = "SALE INVOICE",
			Outlet = location?.Name ?? string.Empty,
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			ReferenceTransactionNo = order?.TransactionNo ?? string.Empty,
			ReferenceDateTime = order?.TransactionDateTime,
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
			new(nameof(SaleItemOverviewModel.ItemName), "Item", exportType, CellAlignment.Left, 0, 30),
			new(nameof(SaleItemOverviewModel.Quantity), "Qty", exportType, CellAlignment.Right, 40, 10, "#,##0.00"),
			new(nameof(SaleItemOverviewModel.Rate), "Rate", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
			new(nameof(SaleItemOverviewModel.DiscountPercent), "Disc %", exportType, CellAlignment.Right, 45, 8, "#,##0.00"),
			new(nameof(SaleItemOverviewModel.AfterDiscount), "Taxable", exportType, CellAlignment.Right, 55, 12, "#,##0.00"),
			new("TaxPercent","Tax %", exportType, CellAlignment.Right, 45, 8, "#,##0.00"),
			new(nameof(SaleItemOverviewModel.TotalTaxAmount), "Tax", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
			new(nameof(SaleItemOverviewModel.Total), "Total", exportType, CellAlignment.Right, 55, 15, "#,##0.00")
		};

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		string fileName = $"SALE_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

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

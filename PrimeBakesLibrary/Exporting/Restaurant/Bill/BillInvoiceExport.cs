using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Restuarant.Bill;
using PrimeBakesLibrary.Models.Restuarant.Dining;
using PrimeBakesLibrary.Models.Store.Masters;
using PrimeBakesLibrary.Models.Store.Product;

namespace PrimeBakesLibrary.Exporting.Restaurant.Bill;

public static class BillInvoiceExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType)
	{
		var transaction = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, transactionId)
			?? throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, transaction.Id);
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId)
			?? throw new InvalidOperationException("Company information is missing.");

		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.LocationId);
		var table = await CommonData.LoadTableDataById<DiningTableModel>(TableNames.DiningTable, transaction.DiningTableId);

		LedgerModel customerLedger = null;
		if (transaction.CustomerId.HasValue && transaction.CustomerId.Value > 0)
		{
			var customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, transaction.CustomerId.Value);
			if (customer is not null)
				customerLedger = new LedgerModel
				{
					Name = customer.Name,
					Phone = customer.Number,
				};
		}

		var allProducts = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

		var lineItems = transactionDetails.Select(detail =>
		{
			var product = allProducts.FirstOrDefault(p => p.Id == detail.ProductId);
			return new
			{
				ItemCategoryId = 0,
				ItemId = detail.ProductId,
				ItemName = product?.Name ?? $"Product #{detail.ProductId}",
				detail.Quantity,
				detail.Rate,
				detail.BaseTotal,
				detail.DiscountPercent,
				detail.DiscountAmount,
				AfterDiscount = detail.DiscountPercent == 0 ? 0 : detail.AfterDiscount,
				CGSTPercent = detail.InclusiveTax ? 0 : detail.CGSTPercent,
				CGSTAmount = detail.InclusiveTax ? 0 : detail.CGSTAmount,
				SGSTPercent = detail.InclusiveTax ? 0 : detail.SGSTPercent,
				SGSTAmount = detail.InclusiveTax ? 0 : detail.SGSTAmount,
				IGSTPercent = detail.InclusiveTax ? 0 : detail.IGSTPercent,
				IGSTAmount = detail.InclusiveTax ? 0 : detail.IGSTAmount,
				TaxPercent = detail.InclusiveTax ? 0 : detail.CGSTPercent + detail.SGSTPercent + detail.IGSTPercent,
				TotalTaxAmount = detail.InclusiveTax ? 0 : detail.TotalTaxAmount,
				detail.InclusiveTax,
				detail.Total,
				detail.NetRate,
				detail.Remarks
			};
		}).ToList();

		var paymentModes = new Dictionary<string, decimal>();
		if (transaction.Cash > 0) paymentModes.Add("Cash", transaction.Cash);
		if (transaction.Card > 0) paymentModes.Add("Card", transaction.Card);
		if (transaction.UPI > 0) paymentModes.Add("UPI", transaction.UPI);
		if (transaction.Credit > 0) paymentModes.Add("Credit", transaction.Credit);

		var invoiceData = new InvoiceData
		{
			Company = company,
			BillTo = customerLedger,
			InvoiceType = "BILL INVOICE",
			Outlet = location?.Name ?? string.Empty,
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			ReferenceTransactionNo = table?.Name ?? string.Empty,
			ReferenceDateTime = null,
			TotalAmount = transaction.TotalAmount,
			Remarks = transaction.Remarks ?? string.Empty,
			Status = transaction.Status,
			PaymentModes = paymentModes
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Sub Total"] = transaction.TotalAfterTax.FormatIndianCurrency(),
			[$"Discount ({transaction.DiscountPercent:0.00}%)"] = transaction.DiscountAmount.FormatIndianCurrency(),
			[$"Service Charge ({transaction.ServiceChargePercent:0.00}% )"] = transaction.ServiceChargeAmount.FormatIndianCurrency(),
			["Round Off"] = transaction.RoundOffAmount.FormatIndianCurrency(),
			["Grand Total"] = transaction.TotalAmount.FormatIndianCurrency()
		};

		var columnSettings = new List<InvoiceColumnSetting>
		{
			new("#", "#", exportType, CellAlignment.Center, 25, 5),
			new(nameof(BillItemCartModel.ItemName), "Item", exportType, CellAlignment.Left, 0, 30),
			new(nameof(BillItemCartModel.Quantity), "Qty", exportType, CellAlignment.Right, 40, 10, "#,##0.00"),
			new(nameof(BillItemCartModel.Rate), "Rate", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
			new(nameof(BillItemCartModel.DiscountPercent), "Disc %", exportType, CellAlignment.Right, 45, 8, "#,##0.00"),
			new(nameof(BillItemCartModel.AfterDiscount), "Taxable", exportType, CellAlignment.Right, 55, 12, "#,##0.00"),
			new("TaxPercent", "Tax %", exportType, CellAlignment.Right, 45, 8, "#,##0.00"),
			new(nameof(BillItemCartModel.TotalTaxAmount), "Tax", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
			new(nameof(BillItemCartModel.Total), "Total", exportType, CellAlignment.Right, 55, 15, "#,##0.00")
		};

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		string fileName = $"BILL_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

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

		var excelStream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
			invoiceData,
			lineItems,
			columnSettings,
			null,
			summaryFields
		);

		fileName += ".xlsx";
		return (excelStream, fileName);
	}
}

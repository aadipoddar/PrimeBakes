using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenProductionInvoiceExcelExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId)
	{
		var transaction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionDetailModel>(TableNames.KitchenProductionDetail, transaction.Id);
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
		var kitchen = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.KitchenId);
		if (company is null || kitchen is null)
			throw new InvalidOperationException("Company or kitchen information is missing.");

		var allProducts = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

		var cartItems = transactionDetails.Select(detail =>
		{
			var product = allProducts.FirstOrDefault(p => p.Id == detail.ProductId);
			return new KitchenProductionProductCartModel
			{
				ProductId = detail.ProductId,
				ProductName = product?.Name ?? $"Product #{detail.ProductId}",
				Quantity = detail.Quantity,
				Rate = detail.Rate,
				Total = detail.Total,
				Remarks = detail.Remarks
			};
		}).ToList();

		// Convert LocationModel to LedgerModel for display
		var kitchenAsLedger = new LedgerModel
		{
			Name = kitchen.Name,
		};

		var invoiceData = new ExcelInvoiceExportUtil.InvoiceData
		{
			Company = company,
			BillTo = kitchenAsLedger,
			InvoiceType = "KITCHEN PRODUCTION INVOICE",
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			TotalAmount = transaction.TotalAmount,
			Remarks = transaction.Remarks,
			Status = transaction.Status,
			PaymentModes = null
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Grand Total"] = transaction.TotalAmount.FormatIndianCurrency()
		};

		var columnSettings = new List<ExcelInvoiceExportUtil.InvoiceColumnSetting>
		{
			new("#", "#", 5, Syncfusion.XlsIO.ExcelHAlign.HAlignCenter),
			new(nameof(KitchenProductionProductCartModel.ProductName), "Item", 40, Syncfusion.XlsIO.ExcelHAlign.HAlignLeft),
			new(nameof(KitchenProductionProductCartModel.Quantity), "Qty", 12, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
			new(nameof(KitchenProductionProductCartModel.Rate), "Rate", 12, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
			new(nameof(KitchenProductionProductCartModel.Total), "Total", 15, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00")
		};

		var stream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
			invoiceData,
			cartItems,
			columnSettings,
			null,
			summaryFields
		);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		string fileName = $"KITCHEN_PRODUCTION_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
		return (stream, fileName);
	}
}

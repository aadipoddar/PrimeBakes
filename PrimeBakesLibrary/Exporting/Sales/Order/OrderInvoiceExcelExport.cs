using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

public static class OrderInvoiceExcelExport
{
	public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId)
	{
		var transaction = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, transaction.Id);
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
		var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.LocationId);
		if (company is null || location is null)
			throw new InvalidOperationException("Company or location information is missing.");

		SaleModel? sale = null;
		if (transaction.SaleId.HasValue)
			sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, transaction.SaleId.Value);

		var allProducts = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

		var cartItems = transactionDetails.Select(detail =>
		{
			var product = allProducts.FirstOrDefault(p => p.Id == detail.ProductId);
			return new OrderItemCartModel
			{
				ItemCategoryId = 0,
				ItemId = detail.ProductId,
				ItemName = product?.Name ?? $"Product #{detail.ProductId}",
				Quantity = detail.Quantity,
				Remarks = detail.Remarks
			};
		}).ToList();

		var invoiceData = new ExcelInvoiceExportUtil.InvoiceData
		{
			Company = company,
			BillTo = null,
			Outlet = location.Name,
			InvoiceType = "ORDER INVOICE",
			TransactionNo = transaction.TransactionNo,
			TransactionDateTime = transaction.TransactionDateTime,
			ReferenceTransactionNo = sale?.TransactionNo ?? string.Empty,
			ReferenceDateTime = sale?.TransactionDateTime,
			TotalAmount = 0, // Orders don't have amounts
			Remarks = transaction.Remarks,
			Status = transaction.Status,
			PaymentModes = null
		};

		var summaryFields = new Dictionary<string, string>
		{
			["Total Items"] = transaction.TotalItems.ToString(),
			["Total Quantity"] = transaction.TotalQuantity.ToString("#,##0.00")
		};

		var columnSettings = new List<ExcelInvoiceExportUtil.InvoiceColumnSetting>
		{
			new("#", "#", 5, Syncfusion.XlsIO.ExcelHAlign.HAlignCenter),
			new(nameof(OrderItemCartModel.ItemName), "Item", 50, Syncfusion.XlsIO.ExcelHAlign.HAlignLeft),
			new(nameof(OrderItemCartModel.Quantity), "Qty", 15, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00")
		};

		var stream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
			invoiceData,
			cartItems,
			columnSettings,
			null,
			summaryFields
		);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		string fileName = $"ORDER_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
		return (stream, fileName);
	}
}

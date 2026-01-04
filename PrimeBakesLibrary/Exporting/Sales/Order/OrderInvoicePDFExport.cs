using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Order;

public static class OrderInvoicePDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId)
    {
        var transaction = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, transactionId) ??
            throw new InvalidOperationException("Transaction not found.");

        var transactionDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, transaction.Id);
        if (transactionDetails is null || transactionDetails.Count == 0)
            throw new InvalidOperationException("No transaction details found for the transaction.");

        var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
        var locationLedger = await LedgerData.LoadLedgerByLocation(transaction.LocationId);
        if (company is null || locationLedger is null)
            throw new InvalidOperationException("Company or location information is missing.");

        SaleModel? sale = null;
        if (transaction.SaleId.HasValue)
            sale = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, transaction.SaleId.Value);

        var allProducts = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

        var lineItems = transactionDetails.Select(detail =>
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

        var invoiceData = new PDFInvoiceExportUtil.InvoiceData
        {
            Company = company,
            BillTo = locationLedger,
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

        var columnSettings = new List<PDFInvoiceExportUtil.InvoiceColumnSetting>
        {
            new("#", "#", 25, Syncfusion.Pdf.Graphics.PdfTextAlignment.Center),
            new(nameof(OrderItemCartModel.ItemName), "Item", 0, Syncfusion.Pdf.Graphics.PdfTextAlignment.Left),
            new(nameof(OrderItemCartModel.Quantity), "Qty", 60, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00")
        };

        var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            lineItems,
            columnSettings,
            null,
            summaryFields
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        string fileName = $"ORDER_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}

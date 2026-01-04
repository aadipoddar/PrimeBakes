using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenProductionInvoicePDFExport
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

        var lineItems = transactionDetails.Select(detail =>
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

        var invoiceData = new PDFInvoiceExportUtil.InvoiceData
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

        var columnSettings = new List<PDFInvoiceExportUtil.InvoiceColumnSetting>
        {
            new("#", "#", 25, Syncfusion.Pdf.Graphics.PdfTextAlignment.Center),
            new(nameof(KitchenProductionProductCartModel.ProductName), "Item", 0, Syncfusion.Pdf.Graphics.PdfTextAlignment.Left),
            new(nameof(KitchenProductionProductCartModel.Quantity), "Qty", 50, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(KitchenProductionProductCartModel.Rate), "Rate", 60, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(KitchenProductionProductCartModel.Total), "Total", 60, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00")
        };

        var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            lineItems,
            columnSettings,
            null,
            summaryFields
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        string fileName = $"KITCHEN_PRODUCTION_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}

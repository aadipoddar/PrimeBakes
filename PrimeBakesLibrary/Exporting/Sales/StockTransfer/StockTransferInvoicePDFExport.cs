using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Exporting.Sales.StockTransfer;

public static class StockTransferInvoicePDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId)
    {
        var transaction = await CommonData.LoadTableDataById<StockTransferModel>(TableNames.StockTransfer, transactionId) ??
            throw new InvalidOperationException("Transaction not found.");

        var transactionDetails = await CommonData.LoadTableDataByMasterId<StockTransferDetailModel>(TableNames.StockTransferDetail, transaction.Id);
        if (transactionDetails is null || transactionDetails.Count == 0)
            throw new InvalidOperationException("No transaction details found for the transaction.");

        var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId) ??
            throw new InvalidOperationException("Company information is missing.");

        var fromLocation = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.LocationId);
        var toLocationLedger = await LedgerData.LoadLedgerByLocation(transaction.ToLocationId);

        var allItems = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

        var cartItems = transactionDetails.Select(detail =>
        {
            var item = allItems.FirstOrDefault(i => i.Id == detail.ProductId);
            return new StockTransferItemCartModel
            {
                ItemId = detail.ProductId,
                ItemName = item?.Name ?? $"Item #{detail.ProductId}",
                Quantity = detail.Quantity,
                Rate = detail.Rate,
                BaseTotal = detail.BaseTotal,
                DiscountPercent = detail.DiscountPercent,
                DiscountAmount = detail.DiscountAmount,
                AfterDiscount = detail.AfterDiscount,
                CGSTPercent = detail.InclusiveTax ? 0 : detail.CGSTPercent,
                CGSTAmount = detail.InclusiveTax ? 0 : detail.CGSTAmount,
                SGSTPercent = detail.InclusiveTax ? 0 : detail.SGSTPercent,
                SGSTAmount = detail.InclusiveTax ? 0 : detail.SGSTAmount,
                IGSTPercent = detail.InclusiveTax ? 0 : detail.IGSTPercent,
                IGSTAmount = detail.InclusiveTax ? 0 : detail.IGSTAmount,
                TotalTaxAmount = detail.InclusiveTax ? 0 : detail.TotalTaxAmount,
                InclusiveTax = detail.InclusiveTax,
                Total = detail.Total
            };
        }).ToList();

        var paymentModes = new Dictionary<string, decimal>();
        if (transaction.Cash > 0) paymentModes.Add("Cash", transaction.Cash);
        if (transaction.Card > 0) paymentModes.Add("Card", transaction.Card);
        if (transaction.UPI > 0) paymentModes.Add("UPI", transaction.UPI);
        if (transaction.Credit > 0) paymentModes.Add("Credit", transaction.Credit);

        var invoiceData = new PDFInvoiceExportUtil.InvoiceData
        {
            Company = company,
            BillTo = toLocationLedger,
            InvoiceType = "STOCK TRANSFER INVOICE",
            Outlet = fromLocation?.Name,
            TransactionNo = transaction.TransactionNo,
            TransactionDateTime = transaction.TransactionDateTime,
            TotalAmount = transaction.TotalAmount,
            Remarks = transaction.Remarks,
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

        var columnSettings = new List<PDFInvoiceExportUtil.InvoiceColumnSetting>
        {
            new("#", "#", 30, Syncfusion.Pdf.Graphics.PdfTextAlignment.Center),
            new(nameof(StockTransferItemCartModel.ItemName), "Item", 150, Syncfusion.Pdf.Graphics.PdfTextAlignment.Left),
            new(nameof(StockTransferItemCartModel.Quantity), "Qty", 50, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.Rate), "Rate", 60, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.BaseTotal), "Amount", 60, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.DiscountPercent), "Disc %", 45, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.AfterDiscount), "Taxable", 60, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.TotalTaxAmount), "Tax", 60, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.Total), "Total", 80, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00")
        };

        var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            cartItems,
            columnSettings,
            null,
            summaryFields
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        string fileName = $"STOCK_TRANSFER_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

public static class PurchaseReturnInvoicePDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId)
    {
        var transaction = await CommonData.LoadTableDataById<PurchaseReturnModel>(TableNames.PurchaseReturn, transactionId) ??
            throw new InvalidOperationException("Transaction not found.");

        var transactionDetails = await CommonData.LoadTableDataByMasterId<PurchaseReturnDetailModel>(TableNames.PurchaseReturnDetail, transaction.Id);
        if (transactionDetails is null || transactionDetails.Count == 0)
            throw new InvalidOperationException("No transaction details found for the transaction.");

        var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
        var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, transaction.PartyId);
        if (company is null || party is null)
            throw new InvalidOperationException("Company or party information is missing.");

        var allItems = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

        var lineItems = transactionDetails.Select(detail =>
        {
            var item = allItems.FirstOrDefault(i => i.Id == detail.RawMaterialId);
            return new PurchaseReturnItemCartModel
            {
                ItemId = detail.RawMaterialId,
                ItemName = item?.Name ?? $"Item #{detail.RawMaterialId}",
                Quantity = detail.Quantity,
                UnitOfMeasurement = detail.UnitOfMeasurement,
                Rate = detail.Rate,
                DiscountPercent = detail.DiscountPercent,
                AfterDiscount = detail.AfterDiscount,
                CGSTPercent = detail.InclusiveTax ? 0 : detail.CGSTPercent,
                SGSTPercent = detail.InclusiveTax ? 0 : detail.SGSTPercent,
                IGSTPercent = detail.InclusiveTax ? 0 : detail.IGSTPercent,
                TotalTaxAmount = detail.InclusiveTax ? 0 : detail.TotalTaxAmount,
                Total = detail.Total
            };
        }).ToList();

        var invoiceData = new PDFInvoiceExportUtil.InvoiceData
        {
            TransactionNo = transaction.TransactionNo,
            Company = company,
            BillTo = party,
            InvoiceType = "PURCHASE RETURN INVOICE",
            TransactionDateTime = transaction.TransactionDateTime,
            TotalAmount = transaction.TotalAmount,
            Remarks = transaction.Remarks,
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

        var columnSettings = new List<PDFInvoiceExportUtil.InvoiceColumnSetting>
        {
            new("#", "#", 25, Syncfusion.Pdf.Graphics.PdfTextAlignment.Center),
            new(nameof(PurchaseReturnItemCartModel.ItemName), "Item", 0, Syncfusion.Pdf.Graphics.PdfTextAlignment.Left),
            new(nameof(PurchaseReturnItemCartModel.UnitOfMeasurement), "UOM", 40, Syncfusion.Pdf.Graphics.PdfTextAlignment.Center),
            new(nameof(PurchaseReturnItemCartModel.Quantity), "Qty", 40, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(PurchaseReturnItemCartModel.Rate), "Rate", 50, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(PurchaseReturnItemCartModel.DiscountPercent), "Disc %", 45, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(PurchaseReturnItemCartModel.AfterDiscount), "Taxable", 55, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(PurchaseReturnItemCartModel.TotalTaxAmount), "Tax", 50, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(PurchaseReturnItemCartModel.Total), "Total", 55, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00")
        };

        var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            lineItems,
            columnSettings,
            null,
            summaryFields
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        string fileName = $"PURCHASE_RETURN_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}

using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

public static class SaleReturnInvoicePDFExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId)
    {
        var transaction = await CommonData.LoadTableDataById<SaleReturnModel>(TableNames.SaleReturn, transactionId) ??
            throw new InvalidOperationException("Transaction not found.");

        var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleReturnDetailModel>(TableNames.SaleReturnDetail, transaction.Id);
        if (transactionDetails is null || transactionDetails.Count == 0)
            throw new InvalidOperationException("No transaction details found for the transaction.");

        var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId) ??
            throw new InvalidOperationException("Company information is missing.");

        var location = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.LocationId);

        LedgerModel party = null;

        if (transaction.PartyId.HasValue)
            party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, transaction.PartyId.Value);
        else if (transaction.CustomerId.HasValue)
        {
            // If no party, convert customer to ledger
            var customer = await CommonData.LoadTableDataById<CustomerModel>(TableNames.Customer, transaction.CustomerId.Value);
            if (customer is not null)
                party = new LedgerModel
                {
                    Name = customer.Name,
                    Phone = customer.Number,
                };
        }

        var allProducts = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

        var lineItems = transactionDetails.Select(detail =>
        {
            var product = allProducts.FirstOrDefault(p => p.Id == detail.ProductId);
            return new SaleReturnItemCartModel
            {
                ItemId = detail.ProductId,
                ItemName = product?.Name ?? $"Product #{detail.ProductId}",
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
                Total = detail.Total,
                NetRate = detail.NetRate,
                Remarks = detail.Remarks
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
            BillTo = party,
            InvoiceType = "SALE RETURN INVOICE",
            Outlet = location?.Name,
            TransactionNo = transaction.TransactionNo,
            TransactionDateTime = transaction.TransactionDateTime,
            ReferenceTransactionNo = null,
            ReferenceDateTime = null,
            TotalAmount = transaction.TotalAmount,
            Remarks = transaction.Remarks,
            Status = transaction.Status,
            PaymentModes = paymentModes
        };

        var summaryFields = new Dictionary<string, string>
        {
            ["Items Total"] = transaction.TotalAfterTax.FormatIndianCurrency(),
            [$"Other Charges ({transaction.OtherChargesPercent:0.00}%)"] = transaction.OtherChargesAmount.FormatIndianCurrency(),
            [$"Discount ({transaction.DiscountPercent:0.00}%)"] = transaction.DiscountAmount.FormatIndianCurrency(),
            ["Round Off"] = transaction.RoundOffAmount.FormatIndianCurrency(),
            ["Grand Total"] = transaction.TotalAmount.FormatIndianCurrency()
        };

        var columnSettings = new List<PDFInvoiceExportUtil.InvoiceColumnSetting>
        {
            new("#", "#", 25, Syncfusion.Pdf.Graphics.PdfTextAlignment.Center),
            new(nameof(SaleReturnItemCartModel.ItemName), "Item", 0, Syncfusion.Pdf.Graphics.PdfTextAlignment.Left),
            new(nameof(SaleReturnItemCartModel.Quantity), "Qty", 40, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(SaleReturnItemCartModel.Rate), "Rate", 50, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(SaleReturnItemCartModel.DiscountPercent), "Disc %", 45, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(SaleReturnItemCartModel.AfterDiscount), "Taxable", 55, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(SaleReturnItemCartModel.TotalTaxAmount), "Tax", 50, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00"),
            new(nameof(SaleReturnItemCartModel.Total), "Total", 55, Syncfusion.Pdf.Graphics.PdfTextAlignment.Right, "#,##0.00")
        };

        var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
            invoiceData,
            lineItems,
            columnSettings,
            null,
            summaryFields
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        string fileName = $"SALE_RETURN_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.pdf";
        return (stream, fileName);
    }
}
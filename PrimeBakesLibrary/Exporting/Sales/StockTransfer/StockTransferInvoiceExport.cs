using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Operations;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Exporting.Sales.StockTransfer;

public static class StockTransferInvoiceExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType)
    {
        var transaction = await CommonData.LoadTableDataById<StockTransferModel>(TableNames.StockTransfer, transactionId) ??
            throw new InvalidOperationException("Transaction not found.");

        var transactionDetails = await CommonData.LoadTableDataByMasterId<StockTransferDetailModel>(TableNames.StockTransferDetail, transaction.Id);
        if (transactionDetails is null || transactionDetails.Count == 0)
            throw new InvalidOperationException("No transaction details found for the transaction.");

        var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId) ??
            throw new InvalidOperationException("Company information is missing.");

        var fromLocation = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.LocationId);
        var toLocationLedger = await LedgerData.LoadLedgerByLocationId(transaction.ToLocationId);

        var allItems = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

        var cartItems = transactionDetails.Select(detail =>
        {
            var item = allItems.FirstOrDefault(i => i.Id == detail.ProductId);
            return new
            {
                ItemId = detail.ProductId,
                ItemName = item?.Name ?? $"Item #{detail.ProductId}",
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
                detail.Total
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

        var columnSettings = new List<InvoiceColumnSetting>
        {
            new("#", "#", exportType, CellAlignment.Center, 30, 5),
            new(nameof(StockTransferItemCartModel.ItemName), "Item", exportType, CellAlignment.Left, 150, 25),
            new(nameof(StockTransferItemCartModel.Quantity), "Qty", exportType, CellAlignment.Right, 50, 10, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.Rate), "Rate", exportType, CellAlignment.Right, 60, 12, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.BaseTotal), "Amount", exportType, CellAlignment.Right, 60, 12, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.DiscountPercent), "Disc %", exportType, CellAlignment.Right, 45, 8, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.DiscountAmount), exportType == InvoiceExportType.PDF ? "Disc Amt" : "Disc Amt", exportType, CellAlignment.Right, 0, 12, "#,##0.00", true),
            new(nameof(StockTransferItemCartModel.AfterDiscount), "Taxable", exportType, CellAlignment.Right, 60, 12, "#,##0.00"),
            new("TaxPercent","Tax %", exportType, CellAlignment.Right, 45, 8, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.TotalTaxAmount), "Tax", exportType, CellAlignment.Right, 60, 12, "#,##0.00"),
            new(nameof(StockTransferItemCartModel.Total), "Total", exportType, CellAlignment.Right, 80, 15, "#,##0.00")
        };

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        string fileName = $"STOCK_TRANSFER_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

        if (exportType == InvoiceExportType.PDF)
        {
            var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
                invoiceData,
                cartItems,
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
                cartItems,
                columnSettings,
                null,
                summaryFields
            );

            fileName += ".xlsx";
            return (stream, fileName);
        }
    }
}

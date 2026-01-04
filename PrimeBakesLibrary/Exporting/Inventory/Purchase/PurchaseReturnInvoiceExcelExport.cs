using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Purchase;

namespace PrimeBakesLibrary.Exporting.Inventory.Purchase;

public static class PurchaseReturnInvoiceExcelExport
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

        var cartItems = transactionDetails.Select(detail =>
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

        var invoiceData = new ExcelInvoiceExportUtil.InvoiceData
        {
            Company = company,
            BillTo = party,
            InvoiceType = "PURCHASE RETURN INVOICE",
            TransactionNo = transaction.TransactionNo,
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

        var columnSettings = new List<ExcelInvoiceExportUtil.InvoiceColumnSetting>
        {
            new("#", "#", 5, Syncfusion.XlsIO.ExcelHAlign.HAlignCenter),
            new(nameof(PurchaseReturnItemCartModel.ItemName), "Item", 30, Syncfusion.XlsIO.ExcelHAlign.HAlignLeft),
            new(nameof(PurchaseReturnItemCartModel.UnitOfMeasurement), "UOM", 8, Syncfusion.XlsIO.ExcelHAlign.HAlignCenter),
            new(nameof(PurchaseReturnItemCartModel.Quantity), "Qty", 10, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
            new(nameof(PurchaseReturnItemCartModel.Rate), "Rate", 12, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
            new(nameof(PurchaseReturnItemCartModel.DiscountPercent), "Disc %", 8, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
            new(nameof(PurchaseReturnItemCartModel.AfterDiscount), "Taxable", 12, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
            new(nameof(PurchaseReturnItemCartModel.TotalTaxAmount), "Tax Amt", 12, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
            new(nameof(PurchaseReturnItemCartModel.Total), "Total", 15, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00")
        };

        var stream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
            invoiceData,
            cartItems,
            columnSettings,
            null,
            summaryFields
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        string fileName = $"PURCHASE_RETURN_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
        return (stream, fileName);
    }
}

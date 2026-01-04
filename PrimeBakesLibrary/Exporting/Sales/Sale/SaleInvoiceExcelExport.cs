using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Product;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Exporting.Sales.Sale;

public static class SaleInvoiceExcelExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId)
    {
        var transaction = await CommonData.LoadTableDataById<SaleModel>(TableNames.Sale, transactionId) ??
            throw new InvalidOperationException("Transaction not found.");

        var transactionDetails = await CommonData.LoadTableDataByMasterId<SaleDetailModel>(TableNames.SaleDetail, transaction.Id);
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

        OrderModel? order = null;
        if (transaction.OrderId.HasValue)
            order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, transaction.OrderId.Value);

        var allProducts = await CommonData.LoadTableData<ProductModel>(TableNames.Product);

        var cartItems = transactionDetails.Select(detail =>
        {
            var product = allProducts.FirstOrDefault(p => p.Id == detail.ProductId);
            return new SaleItemCartModel
            {
                ItemCategoryId = 0,
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

        var invoiceData = new ExcelInvoiceExportUtil.InvoiceData
        {
            Company = company,
            BillTo = party,
            InvoiceType = "SALE INVOICE",
            Outlet = location?.Name,
            TransactionNo = transaction.TransactionNo,
            TransactionDateTime = transaction.TransactionDateTime,
            ReferenceTransactionNo = order?.TransactionNo ?? string.Empty,
            ReferenceDateTime = order?.TransactionDateTime,
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

        var columnSettings = new List<ExcelInvoiceExportUtil.InvoiceColumnSetting>
        {
            new("#", "#", 5, Syncfusion.XlsIO.ExcelHAlign.HAlignCenter),
            new(nameof(SaleItemCartModel.ItemName), "Item", 30, Syncfusion.XlsIO.ExcelHAlign.HAlignLeft),
            new(nameof(SaleItemCartModel.Quantity), "Qty", 10, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
            new(nameof(SaleItemCartModel.Rate), "Rate", 12, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
            new(nameof(SaleItemCartModel.DiscountPercent), "Disc %", 8, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
            new(nameof(SaleItemCartModel.AfterDiscount), "Taxable", 12, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
            new(nameof(SaleItemCartModel.TotalTaxAmount), "Tax Amt", 12, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00"),
            new(nameof(SaleItemCartModel.Total), "Total", 15, Syncfusion.XlsIO.ExcelHAlign.HAlignRight, "#,##0.00")
        };

        var stream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
            invoiceData,
            cartItems,
            columnSettings,
            null,
            summaryFields
        );

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        string fileName = $"SALE_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
        return (stream, fileName);
    }
}

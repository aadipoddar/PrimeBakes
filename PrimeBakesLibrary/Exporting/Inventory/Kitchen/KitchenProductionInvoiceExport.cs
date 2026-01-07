using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Sales.Product;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenProductionInvoiceExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType)
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

        var invoiceData = new InvoiceData
        {
            Company = company,
            BillTo = kitchenAsLedger,
            InvoiceType = "KITCHEN PRODUCTION INVOICE",
            Outlet = kitchen?.Name ?? string.Empty,
            TransactionNo = transaction.TransactionNo,
            TransactionDateTime = transaction.TransactionDateTime,
            TotalAmount = transaction.TotalAmount,
            Remarks = transaction.Remarks ?? string.Empty,
            Status = transaction.Status,
            PaymentModes = null
        };

        var summaryFields = new Dictionary<string, string>
        {
            ["Grand Total"] = transaction.TotalAmount.FormatIndianCurrency()
        };

        var columnSettings = new List<InvoiceColumnSetting>
        {
            new("#", "#", exportType, CellAlignment.Center, 25, 5),
            new(nameof(KitchenProductionProductCartModel.ProductName), "Item", exportType, CellAlignment.Left, 0, 40),
            new(nameof(KitchenProductionProductCartModel.Quantity), "Qty", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
            new(nameof(KitchenProductionProductCartModel.Rate), "Rate", exportType, CellAlignment.Right, 60, 12, "#,##0.00"),
            new(nameof(KitchenProductionProductCartModel.Total), "Total", exportType, CellAlignment.Right, 60, 15, "#,##0.00")
        };

        if (exportType == InvoiceExportType.PDF)
        {
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
        else
        {
            var stream = await ExcelInvoiceExportUtil.ExportInvoiceToExcel(
                invoiceData,
                lineItems,
                columnSettings,
                null,
                summaryFields
            );

            var currentDateTime = await CommonData.LoadCurrentDateTime();
            string fileName = $"KITCHEN_PRODUCTION_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}.xlsx";
            return (stream, fileName);
        }
    }
}

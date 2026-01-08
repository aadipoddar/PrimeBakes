using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakesLibrary.Exporting.Inventory.Kitchen;

public static class KitchenIssueInvoiceExport
{
    public static async Task<(MemoryStream stream, string fileName)> ExportInvoice(int transactionId, InvoiceExportType exportType)
    {
        var transaction = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, transactionId) ??
            throw new InvalidOperationException("Transaction not found.");

        var transactionDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(TableNames.KitchenIssueDetail, transaction.Id);
        if (transactionDetails is null || transactionDetails.Count == 0)
            throw new InvalidOperationException("No transaction details found for the transaction.");

        var company = await CommonData.LoadTableDataById<CompanyModel>(TableNames.Company, transaction.CompanyId);
        var kitchen = await CommonData.LoadTableDataById<LocationModel>(TableNames.Location, transaction.KitchenId);
        if (company is null || kitchen is null)
            throw new InvalidOperationException("Company or kitchen information is missing.");

        var allItems = await CommonData.LoadTableData<RawMaterialModel>(TableNames.RawMaterial);

        var lineItems = transactionDetails.Select(detail =>
        {
            var item = allItems.FirstOrDefault(i => i.Id == detail.RawMaterialId);
            return new KitchenIssueItemCartModel
            {
                ItemId = detail.RawMaterialId,
                ItemName = item?.Name ?? $"Item #{detail.RawMaterialId}",
                Quantity = detail.Quantity,
                UnitOfMeasurement = detail.UnitOfMeasurement,
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
            InvoiceType = "KITCHEN ISSUE INVOICE",
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
            new(nameof(KitchenIssueItemCartModel.ItemName), "Item", exportType, CellAlignment.Left, 0, 35),
            new(nameof(KitchenIssueItemCartModel.UnitOfMeasurement), "UOM", exportType, CellAlignment.Center, 50, 10),
            new(nameof(KitchenIssueItemCartModel.Quantity), "Qty", exportType, CellAlignment.Right, 50, 12, "#,##0.00"),
            new(nameof(KitchenIssueItemCartModel.Rate), "Rate", exportType, CellAlignment.Right, 60, 12, "#,##0.00"),
            new(nameof(KitchenIssueItemCartModel.Total), "Total", exportType, CellAlignment.Right, 60, 15, "#,##0.00")
        };

        var currentDateTime = await CommonData.LoadCurrentDateTime();
        string fileName = $"KITCHEN_ISSUE_INVOICE_{transaction.TransactionNo}_{currentDateTime:yyyyMMdd_HHmmss}";

        if (exportType == InvoiceExportType.PDF)
        {
            var stream = await PDFInvoiceExportUtil.ExportInvoiceToPdf(
                invoiceData,
                lineItems,
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
                lineItems,
                columnSettings,
                null,
                summaryFields
            );

            fileName += ".xlsx";
            return (stream, fileName);
        }
    }
}

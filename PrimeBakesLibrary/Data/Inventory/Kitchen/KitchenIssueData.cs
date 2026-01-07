using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Kitchen;

public static class KitchenIssueData
{
    private static async Task<int> InsertKitchenIssue(KitchenIssueModel kitchenIssue, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenIssue, kitchenIssue, sqlDataAccessTransaction)).FirstOrDefault();

    private static async Task<int> InsertKitchenIssueDetail(KitchenIssueDetailModel kitchenIssueDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenIssueDetail, kitchenIssueDetail, sqlDataAccessTransaction)).FirstOrDefault();

    public static List<KitchenIssueDetailModel> ConvertCartToDetails(List<KitchenIssueItemCartModel> cart, int kitchenIssueId) =>
        [.. cart.Select(item => new KitchenIssueDetailModel
        {
            Id = 0,
            MasterId = kitchenIssueId,
            RawMaterialId = item.ItemId,
            Quantity = item.Quantity,
            UnitOfMeasurement = item.UnitOfMeasurement,
            Rate = item.Rate,
            Total = item.Total,
            Remarks = item.Remarks,
            Status = true
        })];

    public static async Task DeleteTransaction(KitchenIssueModel kitchenIssue)
    {
        using SqlDataAccessTransaction sqlDataAccessTransaction = new();

        try
        {
            sqlDataAccessTransaction.StartTransaction();

            await FinancialYearData.ValidateFinancialYear(kitchenIssue.TransactionDateTime, sqlDataAccessTransaction);

            kitchenIssue.Status = false;
            await InsertKitchenIssue(kitchenIssue, sqlDataAccessTransaction);
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.KitchenIssue), kitchenIssue.Id, sqlDataAccessTransaction);

            sqlDataAccessTransaction.CommitTransaction();

            await KitchenIssueNotify.Notify(kitchenIssue.Id, NotifyType.Deleted);
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }
    }

    public static async Task RecoverTransaction(KitchenIssueModel kitchenIssue)
    {
        kitchenIssue.Status = true;
        var kitchenIssueDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(TableNames.KitchenIssueDetail, kitchenIssue.Id);

        await SaveTransaction(kitchenIssue, null, kitchenIssueDetails, false);

        await KitchenIssueNotify.Notify(kitchenIssue.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(KitchenIssueModel kitchenIssue, List<KitchenIssueItemCartModel> cart, List<KitchenIssueDetailModel> kitchenIssueDetails = null, bool showNotification = true, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        bool update = kitchenIssue.Id > 0;

        if (sqlDataAccessTransaction is null)
        {
            (MemoryStream, string)? previousInvoice = null;
            if (update)
                previousInvoice = await KitchenIssueInvoiceExport.ExportInvoice(kitchenIssue.Id, InvoiceExportType.PDF);

            using SqlDataAccessTransaction newSqlDataAccessTransaction = new();

            try
            {
                newSqlDataAccessTransaction.StartTransaction();
                kitchenIssue.Id = await SaveTransaction(kitchenIssue, cart, kitchenIssueDetails, showNotification, newSqlDataAccessTransaction);
                newSqlDataAccessTransaction.CommitTransaction();
            }
            catch
            {
                newSqlDataAccessTransaction.RollbackTransaction();
                throw;
            }

            if (showNotification)
                await KitchenIssueNotify.Notify(kitchenIssue.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

            return kitchenIssue.Id;
        }

        if (update)
        {
            var existingKitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, kitchenIssue.Id, sqlDataAccessTransaction);
            await FinancialYearData.ValidateFinancialYear(existingKitchenIssue.TransactionDateTime, sqlDataAccessTransaction);
            kitchenIssue.TransactionNo = existingKitchenIssue.TransactionNo;
        }
        else
            kitchenIssue.TransactionNo = await GenerateCodes.GenerateKitchenIssueTransactionNo(kitchenIssue, sqlDataAccessTransaction);

        await FinancialYearData.ValidateFinancialYear(kitchenIssue.TransactionDateTime, sqlDataAccessTransaction);

        kitchenIssue.Id = await InsertKitchenIssue(kitchenIssue, sqlDataAccessTransaction);
        kitchenIssueDetails ??= ConvertCartToDetails(cart, kitchenIssue.Id);
        await SaveTransactionDetail(kitchenIssue, kitchenIssueDetails, update, sqlDataAccessTransaction);
        await SaveRawMaterialStock(kitchenIssue, kitchenIssueDetails, update, sqlDataAccessTransaction);

        return kitchenIssue.Id;
    }

    private static async Task SaveTransactionDetail(KitchenIssueModel kitchenIssue, List<KitchenIssueDetailModel> kitchenIssueDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (kitchenIssueDetails is null || kitchenIssueDetails.Count != kitchenIssue.TotalItems || kitchenIssueDetails.Sum(d => d.Quantity) != kitchenIssue.TotalQuantity)
            throw new InvalidOperationException("Kitchen issue details do not match the transaction summary.");

        if (kitchenIssueDetails.Any(d => !d.Status))
            throw new InvalidOperationException("Kitchen issue detail items must be active.");

        if (update)
        {
            var existingKitchenIssueDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(TableNames.KitchenIssueDetail, kitchenIssue.Id, sqlDataAccessTransaction);
            foreach (var item in existingKitchenIssueDetails)
            {
                item.Status = false;
                await InsertKitchenIssueDetail(item, sqlDataAccessTransaction);
            }
        }

        foreach (var item in kitchenIssueDetails)
        {
            item.MasterId = kitchenIssue.Id;
            var id = await InsertKitchenIssueDetail(item, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save kitchen issue detail item.");
        }
    }

    private static async Task SaveRawMaterialStock(KitchenIssueModel kitchenIssue, List<KitchenIssueDetailModel> kitchenIssueDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.KitchenIssue), kitchenIssue.Id, sqlDataAccessTransaction);

        foreach (var item in kitchenIssueDetails)
        {
            var id = await RawMaterialStockData.InsertRawMaterialStock(new()
            {
                Id = 0,
                RawMaterialId = item.RawMaterialId,
                Quantity = -item.Quantity,
                NetRate = null,
                Type = nameof(StockType.KitchenIssue),
                TransactionId = kitchenIssue.Id,
                TransactionNo = kitchenIssue.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(kitchenIssue.TransactionDateTime)
            }, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save raw material stock entry.");
        }
    }
}
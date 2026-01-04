using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Kitchen;

public static class KitchenIssueData
{
    private static async Task<int> InsertKitchenIssue(KitchenIssueModel kitchenIssue) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenIssue, kitchenIssue)).FirstOrDefault();

    private static async Task<int> InsertKitchenIssueDetail(KitchenIssueDetailModel kitchenIssueDetail) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenIssueDetail, kitchenIssueDetail)).FirstOrDefault();

    public static async Task DeleteTransaction(KitchenIssueModel kitchenIssue)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenIssue.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

        kitchenIssue.Status = false;
        await InsertKitchenIssue(kitchenIssue);
        await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.KitchenIssue), kitchenIssue.Id);
        await SendNotification.KitchenIssueNotification(kitchenIssue.Id, NotificationType.Delete);
    }

    public static async Task RecoverTransaction(KitchenIssueModel kitchenIssue)
    {
        var kitchenIssueDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(TableNames.KitchenIssueDetail, kitchenIssue.Id);
        List<KitchenIssueItemCartModel> kitchenIssueItemCarts = [];

        foreach (var item in kitchenIssueDetails)
            kitchenIssueItemCarts.Add(new()
            {
                ItemId = item.RawMaterialId,
                ItemName = "",
                UnitOfMeasurement = item.UnitOfMeasurement,
                Quantity = item.Quantity,
                Rate = item.Rate,
                Total = item.Total,
                Remarks = item.Remarks
            });

        await SaveTransaction(kitchenIssue, kitchenIssueItemCarts, false);
        await SendNotification.KitchenIssueNotification(kitchenIssue.Id, NotificationType.Recover);
    }

    public static async Task<int> SaveTransaction(KitchenIssueModel kitchenIssue, List<KitchenIssueItemCartModel> kitchenIssueDetails, bool showNotification = true)
    {
        bool update = kitchenIssue.Id > 0;

        if (update)
        {
            var existingKitchenIssue = await CommonData.LoadTableDataById<KitchenIssueModel>(TableNames.KitchenIssue, kitchenIssue.Id);
            var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingKitchenIssue.FinancialYearId);
            if (updateFinancialYear is null || updateFinancialYear.Locked || !updateFinancialYear.Status)
                throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

            kitchenIssue.TransactionNo = existingKitchenIssue.TransactionNo;
        }
        else
            kitchenIssue.TransactionNo = await GenerateCodes.GenerateKitchenIssueTransactionNo(kitchenIssue);

        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenIssue.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

        kitchenIssue.Id = await InsertKitchenIssue(kitchenIssue);
        await SaveTransactionDetail(kitchenIssue, kitchenIssueDetails, update);
        await SaveRawMaterialStock(kitchenIssue, kitchenIssueDetails, update);

        if (showNotification)
            await SendNotification.KitchenIssueNotification(kitchenIssue.Id, update ? NotificationType.Update : NotificationType.Save);

        return kitchenIssue.Id;
    }

    private static async Task SaveTransactionDetail(KitchenIssueModel kitchenIssue, List<KitchenIssueItemCartModel> kitchenIssueDetails, bool update)
    {
        if (update)
        {
            var existingKitchenIssueDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueDetailModel>(TableNames.KitchenIssueDetail, kitchenIssue.Id);
            foreach (var item in existingKitchenIssueDetails)
            {
                item.Status = false;
                await InsertKitchenIssueDetail(item);
            }
        }

        foreach (var item in kitchenIssueDetails)
            await InsertKitchenIssueDetail(new()
            {
                Id = 0,
                MasterId = kitchenIssue.Id,
                RawMaterialId = item.ItemId,
                Quantity = item.Quantity,
                UnitOfMeasurement = item.UnitOfMeasurement,
                Rate = item.Rate,
                Total = item.Total,
                Remarks = item.Remarks,
                Status = true
            });
    }

    private static async Task SaveRawMaterialStock(KitchenIssueModel kitchenIssue, List<KitchenIssueItemCartModel> cart, bool update)
    {
        if (update)
            await RawMaterialStockData.DeleteRawMaterialStockByTypeTransactionId(nameof(StockType.KitchenIssue), kitchenIssue.Id);

        foreach (var item in cart)
            await RawMaterialStockData.InsertRawMaterialStock(new()
            {
                Id = 0,
                RawMaterialId = item.ItemId,
                Quantity = -item.Quantity,
                NetRate = null,
                Type = nameof(StockType.KitchenIssue),
                TransactionId = kitchenIssue.Id,
                TransactionNo = kitchenIssue.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(kitchenIssue.TransactionDateTime)
            });
    }
}
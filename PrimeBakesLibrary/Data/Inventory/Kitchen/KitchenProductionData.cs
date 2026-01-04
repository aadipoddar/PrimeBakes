using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Kitchen;

public static class KitchenProductionData
{
    private static async Task<int> InsertKitchenProduction(KitchenProductionModel kitchenProduction) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenProduction, kitchenProduction)).FirstOrDefault();

    private static async Task<int> InsertKitchenProductionDetail(KitchenProductionDetailModel kitchenProductionDetail) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenProductionDetail, kitchenProductionDetail)).FirstOrDefault();

    public static async Task DeleteTransaction(KitchenProductionModel kitchenProduction)
    {
        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenProduction.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

        kitchenProduction.Status = false;
        await InsertKitchenProduction(kitchenProduction);
        await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.KitchenProduction), kitchenProduction.Id, 1);
        await SendNotification.KitchenProductionNotification(kitchenProduction.Id, NotificationType.Delete);
    }

    public static async Task RecoverTransaction(KitchenProductionModel kitchenProduction)
    {
        var kitchenProductionDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionDetailModel>(TableNames.KitchenProductionDetail, kitchenProduction.Id);
        List<KitchenProductionProductCartModel> kitchenProductionProductCarts = [];

        foreach (var item in kitchenProductionDetails)
            kitchenProductionProductCarts.Add(new()
            {
                ProductId = item.ProductId,
                ProductName = "",
                Quantity = item.Quantity,
                Rate = item.Rate,
                Total = item.Total,
                Remarks = item.Remarks
            });

        await SaveTransaction(kitchenProduction, kitchenProductionProductCarts, false);
        await SendNotification.KitchenProductionNotification(kitchenProduction.Id, NotificationType.Recover);
    }

    public static async Task<int> SaveTransaction(KitchenProductionModel kitchenProduction, List<KitchenProductionProductCartModel> kitchenProductionDetails, bool showNotification = true)
    {
        bool update = kitchenProduction.Id > 0;

        if (update)
        {
            var existingKitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, kitchenProduction.Id);
            var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingKitchenProduction.FinancialYearId);
            if (updateFinancialYear is null || updateFinancialYear.Locked || !updateFinancialYear.Status)
                throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

            kitchenProduction.TransactionNo = existingKitchenProduction.TransactionNo;
        }
        else
            kitchenProduction.TransactionNo = await GenerateCodes.GenerateKitchenProductionTransactionNo(kitchenProduction);

        var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, kitchenProduction.FinancialYearId);
        if (financialYear is null || financialYear.Locked || !financialYear.Status)
            throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

        kitchenProduction.Id = await InsertKitchenProduction(kitchenProduction);
        await SaveTransactionDetail(kitchenProduction, kitchenProductionDetails, update);
        await SaveProductStock(kitchenProduction, kitchenProductionDetails, update);

        if (showNotification)
            await SendNotification.KitchenProductionNotification(kitchenProduction.Id, update ? NotificationType.Update : NotificationType.Save);

        return kitchenProduction.Id;
    }

    private static async Task SaveTransactionDetail(KitchenProductionModel kitchenProduction, List<KitchenProductionProductCartModel> kitchenProductionDetails, bool update)
    {
        if (update)
        {
            var existingKitchenProductionDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionDetailModel>(TableNames.KitchenProductionDetail, kitchenProduction.Id);
            foreach (var item in existingKitchenProductionDetails)
            {
                item.Status = false;
                await InsertKitchenProductionDetail(item);
            }
        }

        foreach (var item in kitchenProductionDetails)
            await InsertKitchenProductionDetail(new()
            {
                Id = 0,
                KitchenProductionId = kitchenProduction.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Rate = item.Rate,
                Total = item.Total,
                Remarks = item.Remarks,
                Status = true
            });
    }

    private static async Task SaveProductStock(KitchenProductionModel kitchenProduction, List<KitchenProductionProductCartModel> cart, bool update)
    {
        if (update)
            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.KitchenProduction), kitchenProduction.Id, 1);

        foreach (var item in cart)
            await ProductStockData.InsertProductStock(new()
            {
                Id = 0,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                NetRate = null,
                Type = nameof(StockType.KitchenProduction),
                TransactionId = kitchenProduction.Id,
                TransactionNo = kitchenProduction.TransactionNo,
                TransactionDate = DateOnly.FromDateTime(kitchenProduction.TransactionDateTime),
                LocationId = 1, // Main Location
            });
    }
}

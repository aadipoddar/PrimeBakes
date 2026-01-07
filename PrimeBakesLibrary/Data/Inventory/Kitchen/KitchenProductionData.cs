using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Data.Inventory.Stock;
using PrimeBakesLibrary.Exporting.Inventory.Kitchen;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Kitchen;

public static class KitchenProductionData
{
    private static async Task<int> InsertKitchenProduction(KitchenProductionModel kitchenProduction, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenProduction, kitchenProduction, sqlDataAccessTransaction)).FirstOrDefault();

    private static async Task<int> InsertKitchenProductionDetail(KitchenProductionDetailModel kitchenProductionDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertKitchenProductionDetail, kitchenProductionDetail, sqlDataAccessTransaction)).FirstOrDefault();

    public static List<KitchenProductionDetailModel> ConvertCartToDetails(List<KitchenProductionProductCartModel> cart, int kitchenProductionId) =>
        [.. cart.Select(item => new KitchenProductionDetailModel
        {
            Id = 0,
            MasterId = kitchenProductionId,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            Rate = item.Rate,
            Total = item.Total,
            Remarks = item.Remarks,
            Status = true
        })];

    public static async Task DeleteTransaction(KitchenProductionModel kitchenProduction)
    {
        using SqlDataAccessTransaction sqlDataAccessTransaction = new();

        try
        {
            sqlDataAccessTransaction.StartTransaction();

            await FinancialYearData.ValidateFinancialYear(kitchenProduction.TransactionDateTime, sqlDataAccessTransaction);

            kitchenProduction.Status = false;
            await InsertKitchenProduction(kitchenProduction, sqlDataAccessTransaction);
            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.KitchenProduction), kitchenProduction.Id, 1, sqlDataAccessTransaction);

            sqlDataAccessTransaction.CommitTransaction();

            await KitchenProductionNotify.Notify(kitchenProduction.Id, NotifyType.Deleted);
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }
    }

    public static async Task RecoverTransaction(KitchenProductionModel kitchenProduction)
    {
        kitchenProduction.Status = true;
        var kitchenProductionDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionDetailModel>(TableNames.KitchenProductionDetail, kitchenProduction.Id);

        await SaveTransaction(kitchenProduction, null, kitchenProductionDetails, false);

        await KitchenProductionNotify.Notify(kitchenProduction.Id, NotifyType.Recovered);
    }

    public static async Task<int> SaveTransaction(KitchenProductionModel kitchenProduction, List<KitchenProductionProductCartModel> cart, List<KitchenProductionDetailModel> kitchenProductionDetails = null, bool showNotification = true, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        bool update = kitchenProduction.Id > 0;

        if (sqlDataAccessTransaction is null)
        {
            (MemoryStream, string)? previousInvoice = null;
            if (update)
                previousInvoice = await KitchenProductionInvoiceExport.ExportInvoice(kitchenProduction.Id, InvoiceExportType.PDF);

            using SqlDataAccessTransaction newSqlDataAccessTransaction = new();

            try
            {
                newSqlDataAccessTransaction.StartTransaction();
                kitchenProduction.Id = await SaveTransaction(kitchenProduction, cart, kitchenProductionDetails, showNotification, newSqlDataAccessTransaction);
                newSqlDataAccessTransaction.CommitTransaction();
            }
            catch
            {
                newSqlDataAccessTransaction.RollbackTransaction();
                throw;
            }

            if (showNotification)
                await KitchenProductionNotify.Notify(kitchenProduction.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

            return kitchenProduction.Id;
        }

        if (update)
        {
            var existingKitchenProduction = await CommonData.LoadTableDataById<KitchenProductionModel>(TableNames.KitchenProduction, kitchenProduction.Id, sqlDataAccessTransaction);
            await FinancialYearData.ValidateFinancialYear(existingKitchenProduction.TransactionDateTime, sqlDataAccessTransaction);
            kitchenProduction.TransactionNo = existingKitchenProduction.TransactionNo;
        }
        else
            kitchenProduction.TransactionNo = await GenerateCodes.GenerateKitchenProductionTransactionNo(kitchenProduction, sqlDataAccessTransaction);

        await FinancialYearData.ValidateFinancialYear(kitchenProduction.TransactionDateTime, sqlDataAccessTransaction);

        kitchenProduction.Id = await InsertKitchenProduction(kitchenProduction, sqlDataAccessTransaction);
        kitchenProductionDetails ??= ConvertCartToDetails(cart, kitchenProduction.Id);
        await SaveTransactionDetail(kitchenProduction, kitchenProductionDetails, update, sqlDataAccessTransaction);
        await SaveProductStock(kitchenProduction, kitchenProductionDetails, update, sqlDataAccessTransaction);

        return kitchenProduction.Id;
    }

    private static async Task SaveTransactionDetail(KitchenProductionModel kitchenProduction, List<KitchenProductionDetailModel> kitchenProductionDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (kitchenProductionDetails is null || kitchenProductionDetails.Count != kitchenProduction.TotalItems || kitchenProductionDetails.Sum(d => d.Quantity) != kitchenProduction.TotalQuantity)
            throw new InvalidOperationException("Kitchen production details do not match the transaction summary.");

        if (kitchenProductionDetails.Any(d => !d.Status))
            throw new InvalidOperationException("Kitchen production detail items must be active.");

        if (update)
        {
            var existingKitchenProductionDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionDetailModel>(TableNames.KitchenProductionDetail, kitchenProduction.Id, sqlDataAccessTransaction);
            foreach (var item in existingKitchenProductionDetails)
            {
                item.Status = false;
                await InsertKitchenProductionDetail(item, sqlDataAccessTransaction);
            }
        }

        foreach (var item in kitchenProductionDetails)
        {
            item.MasterId = kitchenProduction.Id;
            var id = await InsertKitchenProductionDetail(item, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save kitchen production detail item.");
        }
    }

    private static async Task SaveProductStock(KitchenProductionModel kitchenProduction, List<KitchenProductionDetailModel> kitchenProductionDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (update)
            await ProductStockData.DeleteProductStockByTypeTransactionIdLocationId(nameof(StockType.KitchenProduction), kitchenProduction.Id, 1, sqlDataAccessTransaction);

        foreach (var item in kitchenProductionDetails)
        {
            var id = await ProductStockData.InsertProductStock(new()
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
            }, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save product stock entry.");
        }
    }
}

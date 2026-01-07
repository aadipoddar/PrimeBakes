using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Inventory.Stock;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Stock;

public static class RawMaterialStockData
{
    public static async Task<int> InsertRawMaterialStock(RawMaterialStockModel stock, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertRawMaterialStock, stock, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<List<RawMaterialStockSummaryModel>> LoadRawMaterialStockSummaryByDate(DateTime FromDate, DateTime ToDate) =>
        await SqlDataAccess.LoadData<RawMaterialStockSummaryModel, dynamic>(StoredProcedureNames.LoadRawMaterialStockSummaryByDate, new { FromDate = DateOnly.FromDateTime(FromDate), ToDate = DateOnly.FromDateTime(ToDate) });

    public static async Task DeleteRawMaterialStockByTypeTransactionId(string Type, int TransactionId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        await SqlDataAccess.SaveData(StoredProcedureNames.DeleteRawMaterialStockByTypeTransactionId, new { Type, TransactionId }, sqlDataAccessTransaction);

    public static async Task DeleteRawMaterialStockById(int Id, int userId)
    {
        var stock = await CommonData.LoadTableDataById<RawMaterialStockModel>(TableNames.RawMaterialStock, Id);
        if (stock is null)
            return;

        await FinancialYearData.ValidateFinancialYear(stock.TransactionDate.ToDateTime(TimeOnly.MinValue));
        await SqlDataAccess.SaveData(StoredProcedureNames.DeleteRawMaterialStockById, new { Id });
        await RawMaterialStockAdjustmentNotify.Notify(stock, userId, NotifyType.Deleted);
    }

    public static async Task SaveRawMaterialStockAdjustment(DateTime transactionDateTime, List<RawMaterialStockAdjustmentCartModel> cart, int userId)
    {
        var transactionNo = await GenerateCodes.GenerateRawMaterialStockAdjustmentTransactionNo(transactionDateTime);
        var stockSummary = await LoadRawMaterialStockSummaryByDate(transactionDateTime, transactionDateTime);

        if (cart is null || cart.Count == 0)
            throw new InvalidOperationException("Cannot save stock adjustment with no items.");

        await FinancialYearData.ValidateFinancialYear(transactionDateTime);

        using SqlDataAccessTransaction sqlDataAccessTransaction = new();

        try
        {
            sqlDataAccessTransaction.StartTransaction();

            foreach (var item in cart)
            {
                decimal adjustmentQuantity = 0;
                var existingStock = stockSummary.FirstOrDefault(s => s.RawMaterialId == item.RawMaterialId);

                if (existingStock is null)
                    adjustmentQuantity = item.Quantity;
                else
                    adjustmentQuantity = item.Quantity - existingStock.ClosingStock;

                if (adjustmentQuantity != 0)
                {
                    var id = await InsertRawMaterialStock(new()
                    {
                        Id = 0,
                        RawMaterialId = item.RawMaterialId,
                        Quantity = adjustmentQuantity,
                        NetRate = null,
                        TransactionId = null,
                        Type = nameof(StockType.Adjustment),
                        TransactionNo = transactionNo,
                        TransactionDate = DateOnly.FromDateTime(transactionDateTime)
                    }, sqlDataAccessTransaction);

                    if (id <= 0)
                        throw new InvalidOperationException($"Failed to insert stock adjustment for raw material ID {item.RawMaterialId}.");
                }
            }

            sqlDataAccessTransaction.CommitTransaction();

            await RawMaterialStockAdjustmentNotify.Notify(cart.Count, cart.Sum(c => c.Quantity), userId, NotifyType.Created);
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }
    }
}
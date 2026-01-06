using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Inventory.Stock;
using PrimeBakesLibrary.Models.Inventory.Stock;

namespace PrimeBakesLibrary.Data.Inventory.Stock;

public static class ProductStockData
{
    public static async Task<int> InsertProductStock(ProductStockModel stock, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertProductStock, stock, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<List<ProductStockSummaryModel>> LoadProductStockSummaryByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
        await SqlDataAccess.LoadData<ProductStockSummaryModel, dynamic>(StoredProcedureNames.LoadProductStockSummaryByDateLocationId, new { FromDate = DateOnly.FromDateTime(FromDate), ToDate = DateOnly.FromDateTime(ToDate), LocationId });

    public static async Task DeleteProductStockByTypeTransactionIdLocationId(string Type, int TransactionId, int LocationId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        await SqlDataAccess.SaveData(StoredProcedureNames.DeleteProductStockByTypeTransactionIdLocationId, new { Type, TransactionId, LocationId }, sqlDataAccessTransaction);

    public static async Task DeleteProductStockById(int Id, int userId)
    {
        var stock = await CommonData.LoadTableDataById<ProductStockModel>(TableNames.ProductStock, Id);
        if (stock is null)
            return;

        await FinancialYearData.ValidateFinancialYear(stock.TransactionDate.ToDateTime(TimeOnly.MinValue));
        await SqlDataAccess.SaveData(StoredProcedureNames.DeleteProductStockById, new { Id });
        await ProductStockAdjustmentNotify.Notify(stock, userId, NotifyType.Deleted);
    }

    public static async Task SaveProductStockAdjustment(DateTime transactionDateTime, int locationId, List<ProductStockAdjustmentCartModel> cart, int userId)
    {
        var transactionNo = await GenerateCodes.GenerateProductStockAdjustmentTransactionNo(transactionDateTime, locationId);
        var stockSummary = await LoadProductStockSummaryByDateLocationId(transactionDateTime, transactionDateTime, locationId);

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
                var existingStock = stockSummary.FirstOrDefault(s => s.ProductId == item.ProductId);

                if (existingStock is null)
                    adjustmentQuantity = item.Quantity;
                else
                    adjustmentQuantity = item.Quantity - existingStock.ClosingStock;

                if (adjustmentQuantity != 0)
                {
                    var id = await InsertProductStock(new()
                    {
                        Id = 0,
                        ProductId = item.ProductId,
                        Quantity = adjustmentQuantity,
                        NetRate = null,
                        Type = nameof(StockType.Adjustment),
                        TransactionNo = transactionNo,
                        TransactionDate = DateOnly.FromDateTime(transactionDateTime),
                        LocationId = locationId
                    }, sqlDataAccessTransaction);

                    if (id <= 0)
                        throw new InvalidOperationException($"Failed to insert stock adjustment for product ID {item.ProductId}.");
                }
            }

            sqlDataAccessTransaction.CommitTransaction();

            await ProductStockAdjustmentNotify.Notify(cart.Count, cart.Sum(c => c.Quantity), userId, locationId, NotifyType.Created);
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }
    }
}
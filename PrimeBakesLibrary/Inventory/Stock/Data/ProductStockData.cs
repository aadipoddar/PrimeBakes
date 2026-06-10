using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.Stock.Exports;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Inventory.Stock.Data;

public static class ProductStockData
{
	public static async Task<int> InsertProductStock(ProductStockModel stock, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertProductStock, stock, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Product Stock.");

	public static async Task<List<ProductStockSummaryModel>> LoadProductStockSummaryByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId) =>
		await SqlDataAccess.LoadData<ProductStockSummaryModel, dynamic>(InventoryNames.LoadProductStockSummaryByDateLocationId, new { FromDate, ToDate, LocationId });

	public static async Task<int> DeleteProductStockByTypeTransactionIdLocationId(string Type, int TransactionId, int LocationId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.DeleteProductStockByTypeTransactionIdLocationId, new { Type, TransactionId, LocationId }, sqlDataAccessTransaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Product Stock.");

	public static async Task DeleteProductStockById(int Id, int userId, string platform)
	{
		var stock = await CommonData.LoadTableDataById<ProductStockModel>(InventoryNames.ProductStock, Id);
		if (stock is null)
			return;

		await FinancialYearData.ValidateFinancialYear(stock.TransactionDateTime);

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			if ((await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.DeleteProductStockById, new { Id }, transaction)).FirstOrDefault() is not > 0)
				throw new InvalidOperationException("Failed to Delete Product Stock.");

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = InventoryNames.ProductStock,
				RecordNo = stock.TransactionNo,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

		await ProductStockAdjustmentNotify.NotifyDeleted(stock, userId, NotifyType.Deleted);
	}

	#region Save
	private static void ValidateTransaction(DateTime transactionDateTime, int locationId, string transactionNo, List<ProductStockAdjustmentCartModel> cart)
	{
		if (cart is null || cart.Count == 0)
			throw new InvalidOperationException("Please add at least one item to the adjustment before saving.");

		if (transactionDateTime == default)
			throw new InvalidOperationException("Please select a valid transaction date for the adjustment.");

		if (locationId <= 0)
			throw new InvalidOperationException("Please select a valid outlet / location for the adjustment.");

		if (string.IsNullOrWhiteSpace(transactionNo))
			throw new InvalidOperationException("A transaction number could not be generated for the adjustment.");

		if (cart.Any(item => item.ProductId <= 0))
			throw new InvalidOperationException("Each adjustment item must reference a valid product.");
	}

	public static async Task SaveProductStockAdjustment(DateTime transactionDateTime, int locationId, List<ProductStockAdjustmentCartModel> cart, int userId, string platform)
	{
		await FinancialYearData.ValidateFinancialYear(transactionDateTime);

		var transactionNo = await GenerateCodes.GenerateProductStockAdjustmentTransactionNo(transactionDateTime, locationId);
		ValidateTransaction(transactionDateTime, locationId, transactionNo, cart);

		var stockSummary = await LoadProductStockSummaryByDateLocationId(transactionDateTime, transactionDateTime, locationId);

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			foreach (var item in cart)
			{
				var existingStock = stockSummary.FirstOrDefault(s => s.ProductId == item.ProductId);
				var adjustmentQuantity = existingStock is null ? item.Quantity : item.Quantity - existingStock.ClosingStock;

				if (adjustmentQuantity == 0)
					continue;

				await InsertProductStock(new()
				{
					Id = 0,
					ProductId = item.ProductId,
					Quantity = adjustmentQuantity,
					NetRate = item.Rate,
					TransactionId = null,
					Type = nameof(StockType.Adjustment),
					TransactionNo = transactionNo,
					TransactionDateTime = transactionDateTime,
					LocationId = locationId
				}, transaction);
			}

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Insert.ToString(),
				TableName = InventoryNames.ProductStock,
				RecordNo = transactionNo,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

		await ProductStockAdjustmentNotify.NotifyCreated(cart.Count, cart.Sum(c => c.Quantity), transactionNo, userId, locationId, NotifyType.Created);
	}
	#endregion
}
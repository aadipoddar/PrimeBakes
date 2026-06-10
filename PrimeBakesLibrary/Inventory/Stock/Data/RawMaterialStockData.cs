using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.Stock.Exports;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Inventory.Stock.Data;

public static class RawMaterialStockData
{
	public static async Task<int> InsertRawMaterialStock(RawMaterialStockModel stock, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertRawMaterialStock, stock, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Raw Material Stock.");

	public static async Task<List<RawMaterialStockSummaryModel>> LoadRawMaterialStockSummaryByDate(DateTime FromDate, DateTime ToDate) =>
		await SqlDataAccess.LoadData<RawMaterialStockSummaryModel, dynamic>(InventoryNames.LoadRawMaterialStockSummaryByDate, new { FromDate, ToDate });

	public static async Task<int> DeleteRawMaterialStockByTypeTransactionId(string Type, int TransactionId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.DeleteRawMaterialStockByTypeTransactionId, new { Type, TransactionId }, sqlDataAccessTransaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Raw Material Stock.");

	public static async Task DeleteRawMaterialStockById(int Id, int userId, string platform)
	{
		var stock = await CommonData.LoadTableDataById<RawMaterialStockModel>(InventoryNames.RawMaterialStock, Id);
		if (stock is null)
			return;

		await FinancialYearData.ValidateFinancialYear(stock.TransactionDateTime);

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			if ((await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.DeleteRawMaterialStockById, new { Id }, transaction)).FirstOrDefault() is not > 0)
				throw new InvalidOperationException("Failed to Delete Raw Material Stock.");

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Delete.ToString(),
				TableName = InventoryNames.RawMaterialStock,
				RecordNo = stock.TransactionNo,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

		await RawMaterialStockAdjustmentNotify.NotifyDeleted(stock, userId, NotifyType.Deleted);
	}

	#region Save
	private static void ValidateTransaction(DateTime transactionDateTime, string transactionNo, List<RawMaterialStockAdjustmentCartModel> cart)
	{
		if (cart is null || cart.Count == 0)
			throw new InvalidOperationException("Please add at least one item to the adjustment before saving.");

		if (transactionDateTime == default)
			throw new InvalidOperationException("Please select a valid transaction date for the adjustment.");

		if (string.IsNullOrWhiteSpace(transactionNo))
			throw new InvalidOperationException("A transaction number could not be generated for the adjustment.");

		if (cart.Any(item => item.RawMaterialId <= 0))
			throw new InvalidOperationException("Each adjustment item must reference a valid raw material.");
	}

	public static async Task SaveRawMaterialStockAdjustment(DateTime transactionDateTime, List<RawMaterialStockAdjustmentCartModel> cart, int userId, string platform)
	{
		await FinancialYearData.ValidateFinancialYear(transactionDateTime);

		var transactionNo = await GenerateCodes.GenerateRawMaterialStockAdjustmentTransactionNo(transactionDateTime);
		ValidateTransaction(transactionDateTime, transactionNo, cart);

		var stockSummary = await LoadRawMaterialStockSummaryByDate(transactionDateTime, transactionDateTime);

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			foreach (var item in cart)
			{
				var existingStock = stockSummary.FirstOrDefault(s => s.RawMaterialId == item.RawMaterialId);
				var adjustmentQuantity = existingStock is null ? item.Quantity : item.Quantity - existingStock.ClosingStock;

				if (adjustmentQuantity == 0)
					continue;

				await InsertRawMaterialStock(new()
				{
					Id = 0,
					RawMaterialId = item.RawMaterialId,
					Quantity = adjustmentQuantity,
					NetRate = item.Rate,
					TransactionId = null,
					Type = nameof(StockType.Adjustment),
					TransactionNo = transactionNo,
					TransactionDateTime = transactionDateTime
				}, transaction);
			}

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Insert.ToString(),
				TableName = InventoryNames.RawMaterialStock,
				RecordNo = transactionNo,
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});

		await RawMaterialStockAdjustmentNotify.NotifyCreated(cart.Count, cart.Sum(c => c.Quantity), transactionNo, userId, NotifyType.Created);
	}
	#endregion
}
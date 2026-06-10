using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.RawMaterial.Models;
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

	public static async Task<int> DeleteRawMaterialStockByTypeTransactionId(string Type, int TransactionId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.DeleteRawMaterialStockByTypeTransactionId, new { Type, TransactionId }, sqlDataAccessTransaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Raw Material Stock.");

	public static async Task<List<RawMaterialStockModel>> LoadRawMaterialOpeningStockByDate(DateTime FromDate) =>
		await SqlDataAccess.LoadData<RawMaterialStockModel, dynamic>(InventoryNames.LoadRawMaterialOpeningStockByDate, new { FromDate });

	#region Summary
	private static decimal AverageNetRate(IEnumerable<RawMaterialStockModel> stock) =>
		stock.Select(s => s.NetRate).DefaultIfEmpty(0m).Average();

	public static async Task<List<RawMaterialStockSummaryModel>> LoadRawMaterialStockSummaryByDate(DateTime FromDate, DateTime ToDate)
	{
		var daysInPeriod = Math.Max(1, (ToDate.Date - FromDate.Date).Days + 1);

		var rawMaterials = await CommonData.LoadTableDataByStatus<RawMaterialModel>(InventoryNames.RawMaterial);
		var rawMaterialCategories = await CommonData.LoadTableDataByStatus<RawMaterialCategoryModel>(InventoryNames.RawMaterialCategory);
		var stock = await CommonData.LoadTableDataByDate<RawMaterialStockModel>(InventoryNames.RawMaterialStock, FromDate, ToDate);
		var openingStock = await LoadRawMaterialOpeningStockByDate(FromDate.Date);
		var closingStock = await LoadRawMaterialOpeningStockByDate(ToDate.Date.AddDays(1));

		List<RawMaterialStockSummaryModel> summary = [];
		foreach (var item in rawMaterials)
		{
			var itemStock = stock.Where(s => s.RawMaterialId == item.Id).ToList();

			var itemStockSummary = new RawMaterialStockSummaryModel
			{
				RawMaterialId = item.Id,
				RawMaterialName = item.Name,
				RawMaterialCode = item.Code,
				RawMaterialCategoryId = item.RawMaterialCategoryId,
				RawMaterialCategoryName = rawMaterialCategories.FirstOrDefault(c => c.Id == item.RawMaterialCategoryId)?.Name ?? string.Empty,
				UnitOfMeasurement = item.UnitOfMeasurement,

				OpeningStock = openingStock.FirstOrDefault(s => s.RawMaterialId == item.Id)?.Quantity ?? 0,
				InStock = itemStock.Where(s => s.Quantity > 0).Sum(s => s.Quantity),
				OutStock = itemStock.Where(s => s.Quantity < 0).Sum(s => Math.Abs(s.Quantity)),
				ClosingStock = closingStock.FirstOrDefault(s => s.RawMaterialId == item.Id)?.Quantity ?? 0,

				MonthlyStock = itemStock.Sum(s => s.Quantity),
				PurchaseStock = itemStock.Where(s => s.Type == nameof(StockType.Purchase)).Sum(s => s.Quantity),
				PurchaseReturnStock = itemStock.Where(s => s.Type == nameof(StockType.PurchaseReturn)).Sum(s => s.Quantity),
				KitchenIssueStock = itemStock.Where(s => s.Type == nameof(StockType.KitchenIssue)).Sum(s => s.Quantity),
				KitchenProductionStock = itemStock.Where(s => s.Type == nameof(StockType.KitchenProduction)).Sum(s => s.Quantity),
				SaleStock = itemStock.Where(s => s.Type == nameof(StockType.Sale)).Sum(s => s.Quantity),
				SaleReturnStock = itemStock.Where(s => s.Type == nameof(StockType.SaleReturn)).Sum(s => s.Quantity),
				StockTransferStock = itemStock.Where(s => s.Type == nameof(StockType.StockTransfer)).Sum(s => s.Quantity),
				BillStock = itemStock.Where(s => s.Type == nameof(StockType.Bill)).Sum(s => s.Quantity),
				AdjustmentStock = itemStock.Where(s => s.Type == nameof(StockType.Adjustment)).Sum(s => s.Quantity),

				TotalInValue = itemStock.Where(s => s.Quantity > 0).Sum(s => s.Quantity * s.NetRate),
				TotalOutValue = itemStock.Where(s => s.Quantity < 0).Sum(s => Math.Abs(s.Quantity) * s.NetRate),
				PurchaseValue = itemStock.Where(s => s.Type == nameof(StockType.Purchase)).Sum(s => s.Quantity * s.NetRate),
				PurchaseReturnValue = itemStock.Where(s => s.Type == nameof(StockType.PurchaseReturn)).Sum(s => s.Quantity * s.NetRate),
				KitchenIssueValue = itemStock.Where(s => s.Type == nameof(StockType.KitchenIssue)).Sum(s => s.Quantity * s.NetRate),
				KitchenProductionValue = itemStock.Where(s => s.Type == nameof(StockType.KitchenProduction)).Sum(s => s.Quantity * s.NetRate),
				SaleValue = itemStock.Where(s => s.Type == nameof(StockType.Sale)).Sum(s => s.Quantity * s.NetRate),
				SaleReturnValue = itemStock.Where(s => s.Type == nameof(StockType.SaleReturn)).Sum(s => s.Quantity * s.NetRate),
				StockTransferValue = itemStock.Where(s => s.Type == nameof(StockType.StockTransfer)).Sum(s => s.Quantity * s.NetRate),
				BillValue = itemStock.Where(s => s.Type == nameof(StockType.Bill)).Sum(s => s.Quantity * s.NetRate),
				AdjustmentValue = itemStock.Where(s => s.Type == nameof(StockType.Adjustment)).Sum(s => s.Quantity * s.NetRate),

				TransactionCount = itemStock.Count,
				LastTransactionDate = itemStock.Count > 0 ? itemStock.Max(s => s.TransactionDateTime) : null,
				LastPurchaseDate = itemStock.Where(s => s.Type == nameof(StockType.Purchase)) is var purchases && purchases.Any()
					? purchases.Max(s => s.TransactionDateTime) : null,

				Rate = item.Rate,
				AverageInRate = AverageNetRate(itemStock.Where(s => s.Quantity > 0)),
				AverageOutRate = AverageNetRate(itemStock.Where(s => s.Quantity < 0)),

				LastPurchaseRate = itemStock.LastOrDefault(s => s.Type == nameof(StockType.Purchase))?.NetRate ?? 0,
				AveragePurchaseRate = AverageNetRate(itemStock.Where(s => s.Type == nameof(StockType.Purchase))),

				LastPurchaseReturnRate = itemStock.LastOrDefault(s => s.Type == nameof(StockType.PurchaseReturn))?.NetRate ?? 0,
				AveragePurchaseReturnRate = AverageNetRate(itemStock.Where(s => s.Type == nameof(StockType.PurchaseReturn))),

				LastKitchenIssueRate = itemStock.LastOrDefault(s => s.Type == nameof(StockType.KitchenIssue))?.NetRate ?? 0,
				AverageKitchenIssueRate = AverageNetRate(itemStock.Where(s => s.Type == nameof(StockType.KitchenIssue))),

				LastKitchenProductionRate = itemStock.LastOrDefault(s => s.Type == nameof(StockType.KitchenProduction))?.NetRate ?? 0,
				AverageKitchenProductionRate = AverageNetRate(itemStock.Where(s => s.Type == nameof(StockType.KitchenProduction))),

				LastSaleRate = itemStock.LastOrDefault(s => s.Type == nameof(StockType.Sale))?.NetRate ?? 0,
				AverageSaleRate = AverageNetRate(itemStock.Where(s => s.Type == nameof(StockType.Sale))),

				LastSaleReturnRate = itemStock.LastOrDefault(s => s.Type == nameof(StockType.SaleReturn))?.NetRate ?? 0,
				AverageSaleReturnRate = AverageNetRate(itemStock.Where(s => s.Type == nameof(StockType.SaleReturn))),

				LastStockTransferRate = itemStock.LastOrDefault(s => s.Type == nameof(StockType.StockTransfer))?.NetRate ?? 0,
				AverageStockTransferRate = AverageNetRate(itemStock.Where(s => s.Type == nameof(StockType.StockTransfer))),

				LastBillRate = itemStock.LastOrDefault(s => s.Type == nameof(StockType.Bill))?.NetRate ?? 0,
				AverageBillRate = AverageNetRate(itemStock.Where(s => s.Type == nameof(StockType.Bill))),

				LastAdjustmentRate = itemStock.LastOrDefault(s => s.Type == nameof(StockType.Adjustment))?.NetRate ?? 0,
				AverageAdjustmentRate = AverageNetRate(itemStock.Where(s => s.Type == nameof(StockType.Adjustment)))
			};

			itemStockSummary.OpeningValue = itemStockSummary.OpeningStock * itemStockSummary.Rate;
			itemStockSummary.ClosingValueByRate = itemStockSummary.ClosingStock * itemStockSummary.Rate;
			itemStockSummary.ClosingValueByAverageInRate = itemStockSummary.ClosingStock * itemStockSummary.AverageInRate;
			itemStockSummary.ClosingValueByAverageOutRate = itemStockSummary.ClosingStock * itemStockSummary.AverageOutRate;

			itemStockSummary.AverageDailyConsumption = itemStockSummary.OutStock / daysInPeriod;
			itemStockSummary.DaysOnHand = itemStockSummary.AverageDailyConsumption > 0
				? itemStockSummary.ClosingStock / itemStockSummary.AverageDailyConsumption : 0;

			var averageStock = (itemStockSummary.OpeningStock + itemStockSummary.ClosingStock) / 2;
			itemStockSummary.StockTurnoverRatio = averageStock != 0 ? itemStockSummary.OutStock / averageStock : 0;

			itemStockSummary.IsNegativeStock = itemStockSummary.ClosingStock < 0;
			itemStockSummary.RateVariance = itemStockSummary.ClosingValueByRate - itemStockSummary.ClosingValueByAverageInRate;

			summary.Add(itemStockSummary);
		}
		return summary;
	}
	#endregion

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
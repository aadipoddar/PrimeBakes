using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Inventory.Kitchen.Models;
using PrimeBakesLibrary.Inventory.Stock.Exports;
using PrimeBakesLibrary.Inventory.Stock.Models;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Operations.Location;
using PrimeBakesLibrary.Restaurant.Bill.Models;
using PrimeBakesLibrary.Store.Product.Models;
using PrimeBakesLibrary.Store.Sale.Models;
using PrimeBakesLibrary.Store.StockTransfer.Models;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Inventory.Stock.Data;

public static class ProductStockData
{
	public static async Task<int> InsertProductStock(ProductStockModel stock, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertProductStock, stock, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Product Stock.");

	public static async Task<int> DeleteProductStockByTransactionNo(string TransactionNo, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.DeleteProductStockByTransactionNo, new { TransactionNo }, sqlDataAccessTransaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Product Stock.");

	public static async Task<int> DeleteProductStockById(int Id, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.DeleteProductStockById, new { Id }, sqlDataAccessTransaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Product Stock.");

	public static async Task<List<ProductStockModel>> LoadProductOpeningStockByDateLocationId(DateTime FromDate, int LocationId) =>
		await SqlDataAccess.LoadData<ProductStockModel, dynamic>(InventoryNames.LoadProductOpeningStockByDateLocationId, new { FromDate, LocationId });

	#region Summary
	private static decimal AverageNetRate(IEnumerable<ProductStockModel> stock) =>
		stock.Select(s => s.NetRate).DefaultIfEmpty(0m).Average();

	public static async Task<List<ProductStockSummaryModel>> LoadProductStockSummaryByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId)
	{
		var daysInPeriod = Math.Max(1, (ToDate.Date - FromDate.Date).Days + 1);

		var products = await CommonData.LoadTableDataByStatus<ProductModel>(StoreNames.Product);
		var productCategories = await CommonData.LoadTableDataByStatus<ProductCategoryModel>(StoreNames.ProductCategory);
		var productLocations = await CommonData.LoadTableData<ProductLocationModel>(StoreNames.ProductLocation);
		var location = await CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, LocationId);

		var stock = (await CommonData.LoadTableDataByDate<ProductStockModel>(InventoryNames.ProductStock, FromDate, ToDate))
			.Where(s => s.LocationId == LocationId).ToList();
		var openingStock = await LoadProductOpeningStockByDateLocationId(FromDate.Date, LocationId);
		var closingStock = await LoadProductOpeningStockByDateLocationId(ToDate.Date.AddDays(1), LocationId);

		List<ProductStockSummaryModel> summary = [];
		foreach (var item in products)
		{
			var itemStock = stock.Where(s => s.ProductId == item.Id).ToList();
			var rate = productLocations.FirstOrDefault(l => l.ProductId == item.Id && l.LocationId == LocationId)?.Rate ?? item.Rate;

			var itemStockSummary = new ProductStockSummaryModel
			{
				ProductId = item.Id,
				ProductName = item.Name,
				ProductCode = item.Code,
				ProductCategoryId = item.ProductCategoryId,
				ProductCategoryName = productCategories.FirstOrDefault(c => c.Id == item.ProductCategoryId)?.Name ?? string.Empty,
				LocationId = LocationId,
				LocationName = location?.Name ?? string.Empty,

				OpeningStock = openingStock.FirstOrDefault(s => s.ProductId == item.Id)?.Quantity ?? 0,
				InStock = itemStock.Where(s => s.Quantity > 0).Sum(s => s.Quantity),
				OutStock = itemStock.Where(s => s.Quantity < 0).Sum(s => Math.Abs(s.Quantity)),
				ClosingStock = closingStock.FirstOrDefault(s => s.ProductId == item.Id)?.Quantity ?? 0,

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
				LastSaleDate = itemStock.Where(s => s.Type == nameof(StockType.Sale)) is var sales && sales.Any()
					? sales.Max(s => s.TransactionDateTime) : null,

				Rate = rate,
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

	#region Delete
	public static async Task DeleteProductStockAdjustment(int id, int userId, string platform)
	{
		var stock = await CommonData.LoadTableDataById<ProductStockModel>(InventoryNames.ProductStock, id);
		if (stock is null)
			return;

		await FinancialYearData.ValidateFinancialYear(stock.TransactionDateTime);

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			await DeleteProductStockById(id, transaction);

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
	#endregion

	#region Recalculate
	public static async Task RecalculateStockByDateLocation(DateTime fromDate, DateTime toDate, int locationId, bool deleteAdjustments, int userId, string platform)
	{
		await FinancialYearData.ValidateFinancialYear(fromDate);
		await FinancialYearData.ValidateFinancialYear(toDate);

		var stock = (await CommonData.LoadTableDataByDate<ProductStockModel>(InventoryNames.ProductStock, fromDate, toDate))
			.Where(s => s.LocationId == locationId).ToList();

		var ledger = await LocationData.LoadLedgerByLocationId(locationId);
		var kitchenProductions = await CommonData.LoadTableDataByDate<KitchenProductionItemOverviewModel>(InventoryNames.KitchenProductionItemOverview, fromDate, toDate);
		var sales = await CommonData.LoadTableDataByDate<SaleItemOverviewModel>(StoreNames.SaleItemOverview, fromDate, toDate);
		var saleReturns = await CommonData.LoadTableDataByDate<SaleReturnItemOverviewModel>(StoreNames.SaleReturnItemOverview, fromDate, toDate);
		var stockTransfers = await CommonData.LoadTableDataByDate<StockTransferItemOverviewModel>(StoreNames.StockTransferItemOverview, fromDate, toDate);
		var bills = await CommonData.LoadTableDataByDate<BillItemOverviewModel>(RestaurantNames.BillItemOverview, fromDate, toDate);

		kitchenProductions = locationId == 1 ? [.. kitchenProductions.Where(s => s.MasterStatus)] : [];
		sales = [.. sales.Where(s => (s.LocationId == locationId || s.PartyId == ledger.Id) && s.MasterStatus)];
		saleReturns = [.. saleReturns.Where(s => (s.LocationId == locationId || s.PartyId == ledger.Id) && s.MasterStatus)];
		stockTransfers = [.. stockTransfers.Where(s => (s.LocationId == locationId || s.ToLocationId == locationId) && s.MasterStatus)];
		bills = [.. bills.Where(s => s.LocationId == locationId && s.MasterStatus)];

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			foreach (var item in stock)
				if (item.Type == nameof(StockType.Adjustment) && deleteAdjustments || item.Type != nameof(StockType.Adjustment))
					await DeleteProductStockById(item.Id, transaction);

			foreach (var item in kitchenProductions)
				await InsertProductStock(new()
				{
					Id = 0,
					ProductId = item.ItemId,
					Quantity = item.Quantity,
					NetRate = item.Rate,
					Type = nameof(StockType.KitchenProduction),
					TransactionId = item.MasterId,
					TransactionNo = item.TransactionNo,
					TransactionDateTime = item.TransactionDateTime,
					LocationId = 1, // Main Location
				}, transaction);

			foreach (var item in sales)
			{
				if (item.LocationId == locationId)
					await InsertProductStock(new()
					{
						Id = 0,
						ProductId = item.ItemId,
						Quantity = -item.Quantity,
						NetRate = item.NetRate,
						Type = nameof(StockType.Sale),
						TransactionId = item.MasterId,
						TransactionNo = item.TransactionNo,
						TransactionDateTime = item.TransactionDateTime,
						LocationId = locationId
					}, transaction);

				if (item.PartyId is not null && item.PartyId == ledger.Id)
					await InsertProductStock(new()
					{
						Id = 0,
						ProductId = item.ItemId,
						Quantity = item.Quantity,
						NetRate = item.NetRate,
						Type = nameof(StockType.Purchase),
						TransactionId = item.MasterId,
						TransactionNo = item.TransactionNo,
						TransactionDateTime = item.TransactionDateTime,
						LocationId = locationId
					}, transaction);
			}

			foreach (var item in saleReturns)
			{
				if (item.LocationId == locationId)
					await InsertProductStock(new()
					{
						Id = 0,
						ProductId = item.ItemId,
						Quantity = item.Quantity,
						NetRate = item.NetRate,
						Type = nameof(StockType.SaleReturn),
						TransactionId = item.MasterId,
						TransactionNo = item.TransactionNo,
						TransactionDateTime = item.TransactionDateTime,
						LocationId = locationId
					}, transaction);

				if (item.PartyId is not null && item.PartyId == ledger.Id)
					await InsertProductStock(new()
					{
						Id = 0,
						ProductId = item.ItemId,
						Quantity = -item.Quantity,
						NetRate = item.NetRate,
						Type = nameof(StockType.PurchaseReturn),
						TransactionId = item.MasterId,
						TransactionNo = item.TransactionNo,
						TransactionDateTime = item.TransactionDateTime,
						LocationId = locationId
					}, transaction);
			}

			foreach (var item in stockTransfers)
			{
				if (item.LocationId == locationId)
					await InsertProductStock(new()
					{
						Id = 0,
						ProductId = item.ItemId,
						Quantity = -item.Quantity,
						NetRate = item.NetRate,
						Type = nameof(StockType.StockTransfer),
						TransactionId = item.MasterId,
						TransactionNo = item.TransactionNo,
						TransactionDateTime = item.TransactionDateTime,
						LocationId = locationId
					}, transaction);

				if (item.ToLocationId == locationId)
					await InsertProductStock(new()
					{
						Id = 0,
						ProductId = item.ItemId,
						Quantity = item.Quantity,
						NetRate = item.NetRate,
						Type = nameof(StockType.StockTransfer),
						TransactionId = item.MasterId,
						TransactionNo = item.TransactionNo,
						TransactionDateTime = item.TransactionDateTime,
						LocationId = locationId
					}, transaction);
			}

			foreach (var item in bills)
				await ProductStockData.InsertProductStock(new()
				{
					Id = 0,
					ProductId = item.ItemId,
					Quantity = -item.Quantity,
					NetRate = item.NetRate,
					Type = nameof(StockType.Bill),
					TransactionId = item.MasterId,
					TransactionNo = item.TransactionNo,
					TransactionDateTime = item.TransactionDateTime,
					LocationId = item.LocationId
				}, transaction);

			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Update.ToString(),
				TableName = InventoryNames.ProductStock,
				RecordNo = $"Recalculate {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd} for LocationId {locationId}",
				CreatedBy = userId,
				CreatedFromPlatform = platform
			}, transaction);
		});
	}
	#endregion

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
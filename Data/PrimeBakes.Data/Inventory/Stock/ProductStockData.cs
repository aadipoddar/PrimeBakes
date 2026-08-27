using Dapper;

using System.Data;

using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Operations.Location;
using PrimeBakes.Data.Store.Product;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Inventory.Stock;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.Location;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Product;
using PrimeBakes.Models.Store.Sale;
using PrimeBakes.Models.Store.StockTransfer;

namespace PrimeBakes.Data.Inventory.Stock;

public static class ProductStockData
{
	public static async Task<int> InsertProductStock(ProductStockModel stock, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertProductStock, stock, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Product Stock.");

	public static async Task InsertProductStockList(DataTable productStocks, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertProductStockList, new { ProductStocks = productStocks.AsTableValuedParameter(InventoryNames.ProductStockType) }, sqlDataAccessTransaction);

	private static async Task<int> DeleteProductStockById(int Id, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.DeleteProductStockById, new { Id }, sqlDataAccessTransaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Product Stock.");

	public static async Task<int> DeleteProductStockByTransactionNo(string TransactionNo, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.DeleteProductStockByTransactionNo, new { TransactionNo }, sqlDataAccessTransaction)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Product Stock.");

	public static async Task<List<ProductStockModel>> LoadProductOpeningStockByDateLocationId(DateTime FromDate, int LocationId) =>
		await SqlDataAccess.LoadData<ProductStockModel, dynamic>(InventoryNames.LoadProductOpeningStockByDateLocationId, new { FromDate, LocationId });

	#region Summary
	private static decimal AverageNetRate(IEnumerable<ProductStockModel> stock) =>
		stock.Select(s => s.NetRate).DefaultIfEmpty(0m).Average();

	public static async Task<List<ProductStockSummaryModel>> LoadProductStockSummaryByDateLocationId(DateTime FromDate, DateTime ToDate, int LocationId)
	{
		var daysInPeriod = Math.Max(1, (ToDate.Date - FromDate.Date).Days + 1);

		var productsTask = CommonData.LoadTableDataByStatus<ProductModel>(StoreNames.Product);
		var productCategoriesTask = CommonData.LoadTableDataByStatus<ProductCategoryModel>(StoreNames.ProductCategory);
		var productLocationsTask = ProductLocationData.LoadProductLocationOverviewByProductLocationDate(LocationId: LocationId, Date: DateOnly.FromDateTime(ToDate.Date));
		var locationTask = CommonData.LoadTableDataById<LocationModel>(OperationNames.Location, LocationId);
		var stockTask = CommonData.LoadTableDataByDate<ProductStockModel>(InventoryNames.ProductStock, FromDate, ToDate);
		var openingStockTask = LoadProductOpeningStockByDateLocationId(FromDate.Date, LocationId);
		var closingStockTask = LoadProductOpeningStockByDateLocationId(ToDate.Date.AddDays(1), LocationId);

		var products = await productsTask;
		var productCategories = await productCategoriesTask;
		var productLocations = await productLocationsTask;
		var location = await locationTask;
		var stock = (await stockTask).Where(s => s.LocationId == LocationId).ToList();
		var openingStock = await openingStockTask;
		var closingStock = await closingStockTask;

		var stockByProduct = stock.ToLookup(s => s.ProductId);
		var openingByProduct = openingStock.ToLookup(s => s.ProductId);
		var closingByProduct = closingStock.ToLookup(s => s.ProductId);
		var productLocationByProduct = productLocations.ToLookup(l => l.ProductId);
		var categoryById = productCategories.ToLookup(c => c.Id);

		List<ProductStockSummaryModel> summary = [];
		foreach (var item in products)
		{
			var itemStock = stockByProduct[item.Id].ToList();
			var rate = productLocationByProduct[item.Id].FirstOrDefault()?.Rate ?? item.Rate;

			var itemStockSummary = new ProductStockSummaryModel
			{
				ProductId = item.Id,
				ProductName = item.Name,
				ProductCode = item.Code,
				ProductCategoryId = item.ProductCategoryId,
				ProductCategoryName = categoryById[item.ProductCategoryId].FirstOrDefault()?.Name ?? string.Empty,
				LocationId = LocationId,
				LocationName = location?.Name ?? string.Empty,

				OpeningStock = openingByProduct[item.Id].FirstOrDefault()?.Quantity ?? 0,
				InStock = itemStock.Where(s => s.Quantity > 0).Sum(s => s.Quantity),
				OutStock = itemStock.Where(s => s.Quantity < 0).Sum(s => Math.Abs(s.Quantity)),
				ClosingStock = closingByProduct[item.Id].FirstOrDefault()?.Quantity ?? 0,

				MonthlyStock = itemStock.Sum(s => s.Quantity),
				PurchaseStock = itemStock.Where(s => s.Type == nameof(StockType.Purchase)).Sum(s => s.Quantity),
				PurchaseReturnStock = itemStock.Where(s => s.Type == nameof(StockType.PurchaseReturn)).Sum(s => s.Quantity),
				KitchenIssueStock = itemStock.Where(s => s.Type == nameof(StockType.KitchenIssue)).Sum(s => s.Quantity),
				KitchenProductionStock = itemStock.Where(s => s.Type == nameof(StockType.KitchenProduction)).Sum(s => s.Quantity),
				KitchenProductionReturnStock = itemStock.Where(s => s.Type == nameof(StockType.KitchenProductionReturn)).Sum(s => s.Quantity),
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
				KitchenProductionReturnValue = itemStock.Where(s => s.Type == nameof(StockType.KitchenProductionReturn)).Sum(s => s.Quantity * s.NetRate),
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

				LastKitchenProductionReturnRate = itemStock.LastOrDefault(s => s.Type == nameof(StockType.KitchenProductionReturn))?.NetRate ?? 0,
				AverageKitchenProductionReturnRate = AverageNetRate(itemStock.Where(s => s.Type == nameof(StockType.KitchenProductionReturn))),

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
		var kitchenProductionReturns = await CommonData.LoadTableDataByDate<KitchenProductionReturnItemOverviewModel>(InventoryNames.KitchenProductionReturnItemOverview, fromDate, toDate);
		var sales = await CommonData.LoadTableDataByDate<SaleItemOverviewModel>(StoreNames.SaleItemOverview, fromDate, toDate);
		var saleReturns = await CommonData.LoadTableDataByDate<SaleReturnItemOverviewModel>(StoreNames.SaleReturnItemOverview, fromDate, toDate);
		var stockTransfers = await CommonData.LoadTableDataByDate<StockTransferItemOverviewModel>(StoreNames.StockTransferItemOverview, fromDate, toDate);
		var bills = await CommonData.LoadTableDataByDate<BillItemOverviewModel>(RestaurantNames.BillItemOverview, fromDate, toDate);

		kitchenProductions = locationId == 1 ? [.. kitchenProductions.Where(s => s.MasterStatus)] : [];
		kitchenProductionReturns = locationId == 1 ? [.. kitchenProductionReturns.Where(s => s.MasterStatus)] : [];
		sales = [.. sales.Where(s => (s.LocationId == locationId || s.PartyId == ledger.Id) && s.MasterStatus)];
		saleReturns = [.. saleReturns.Where(s => (s.LocationId == locationId || s.PartyId == ledger.Id) && s.MasterStatus)];
		stockTransfers = [.. stockTransfers.Where(s => (s.LocationId == locationId || s.ToLocationId == locationId) && s.MasterStatus)];
		bills = [.. bills.Where(s => s.LocationId == locationId && s.MasterStatus)];

		await SqlDataAccessTransaction.Run(async transaction =>
		{
			foreach (var item in stock)
				if (item.Type == nameof(StockType.Adjustment) && deleteAdjustments || item.Type != nameof(StockType.Adjustment))
					await DeleteProductStockById(item.Id, transaction);

			List<ProductStockModel> stocks = [];

			foreach (var item in kitchenProductions)
				stocks.Add(new()
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
				});

			foreach (var item in kitchenProductionReturns)
				stocks.Add(new()
				{
					Id = 0,
					ProductId = item.ItemId,
					Quantity = -item.Quantity,
					NetRate = item.Rate,
					Type = nameof(StockType.KitchenProductionReturn),
					TransactionId = item.MasterId,
					TransactionNo = item.TransactionNo,
					TransactionDateTime = item.TransactionDateTime,
					LocationId = 1, // Main Location
				});

			foreach (var item in sales)
			{
				if (item.LocationId == locationId)
					stocks.Add(new()
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
					});

				if (item.PartyId is not null && item.PartyId == ledger.Id)
					stocks.Add(new()
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
					});
			}

			foreach (var item in saleReturns)
			{
				if (item.LocationId == locationId)
					stocks.Add(new()
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
					});

				if (item.PartyId is not null && item.PartyId == ledger.Id)
					stocks.Add(new()
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
					});
			}

			foreach (var item in stockTransfers)
			{
				if (item.LocationId == locationId)
					stocks.Add(new()
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
					});

				if (item.ToLocationId == locationId)
					stocks.Add(new()
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
					});
			}

			foreach (var item in bills)
				stocks.Add(new()
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
				});

			await InsertProductStockList(SqlDataAccess.ToDataTable(stocks), transaction);

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
			List<ProductStockModel> stocks = [];

			foreach (var item in cart)
			{
				var existingStock = stockSummary.FirstOrDefault(s => s.ProductId == item.ProductId);
				var adjustmentQuantity = existingStock is null ? item.Quantity : item.Quantity - existingStock.ClosingStock;

				if (adjustmentQuantity == 0)
					continue;

				stocks.Add(new()
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
				});
			}

			await InsertProductStockList(SqlDataAccess.ToDataTable(stocks), transaction);

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
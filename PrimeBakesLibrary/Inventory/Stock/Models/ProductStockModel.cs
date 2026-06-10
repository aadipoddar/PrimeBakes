namespace PrimeBakesLibrary.Inventory.Stock.Models;

public class ProductStockModel
{
	public int Id { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public decimal NetRate { get; set; }
	public string Type { get; set; }
	public int? TransactionId { get; set; }
	public string TransactionNo { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int LocationId { get; set; }
}

public class ProductStockAdjustmentCartModel
{
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public decimal Stock { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal Total { get; set; }
}

public class ProductStockDetailsModel
{
	public int Id { get; set; }
	public int ProductId { get; set; }
	public string ProductCode { get; set; }
	public string ProductName { get; set; }
	public decimal Quantity { get; set; }
	public decimal NetRate { get; set; }
	public decimal Total { get; set; }
	public string Type { get; set; }
	public int? TransactionId { get; set; }
	public string TransactionNo { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int LocationId { get; set; }
	public string LocationName { get; set; }
}

public class ProductStockSummaryModel
{
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public string ProductCode { get; set; }
	public int ProductCategoryId { get; set; }
	public string ProductCategoryName { get; set; }
	public int LocationId { get; set; }
	public string LocationName { get; set; }

	public decimal OpeningStock { get; set; }
	public decimal InStock { get; set; }
	public decimal OutStock { get; set; }
	public decimal ClosingStock { get; set; }

	public decimal MonthlyStock { get; set; }
	public decimal PurchaseStock { get; set; }
	public decimal PurchaseReturnStock { get; set; }
	public decimal KitchenIssueStock { get; set; }
	public decimal KitchenProductionStock { get; set; }
	public decimal SaleStock { get; set; }
	public decimal SaleReturnStock { get; set; }
	public decimal StockTransferStock { get; set; }
	public decimal BillStock { get; set; }
	public decimal AdjustmentStock { get; set; }

	public decimal OpeningValue { get; set; }
	public decimal TotalInValue { get; set; }
	public decimal TotalOutValue { get; set; }
	public decimal PurchaseValue { get; set; }
	public decimal PurchaseReturnValue { get; set; }
	public decimal KitchenIssueValue { get; set; }
	public decimal KitchenProductionValue { get; set; }
	public decimal SaleValue { get; set; }
	public decimal SaleReturnValue { get; set; }
	public decimal StockTransferValue { get; set; }
	public decimal BillValue { get; set; }
	public decimal AdjustmentValue { get; set; }

	public int TransactionCount { get; set; }
	public DateTime? LastTransactionDate { get; set; }
	public DateTime? LastSaleDate { get; set; }

	public decimal AverageDailyConsumption { get; set; }
	public decimal DaysOnHand { get; set; }
	public decimal StockTurnoverRatio { get; set; }
	public bool IsNegativeStock { get; set; }
	public decimal RateVariance { get; set; }

	public decimal Rate { get; set; }
	public decimal ClosingValueByRate { get; set; }

	public decimal AverageInRate { get; set; }
	public decimal ClosingValueByAverageInRate { get; set; }

	public decimal AverageOutRate { get; set; }
	public decimal ClosingValueByAverageOutRate { get; set; }

	public decimal LastPurchaseRate { get; set; }
	public decimal AveragePurchaseRate { get; set; }

	public decimal LastPurchaseReturnRate { get; set; }
	public decimal AveragePurchaseReturnRate { get; set; }

	public decimal LastKitchenIssueRate { get; set; }
	public decimal AverageKitchenIssueRate { get; set; }

	public decimal LastKitchenProductionRate { get; set; }
	public decimal AverageKitchenProductionRate { get; set; }

	public decimal LastSaleRate { get; set; }
	public decimal AverageSaleRate { get; set; }

	public decimal LastSaleReturnRate { get; set; }
	public decimal AverageSaleReturnRate { get; set; }

	public decimal LastStockTransferRate { get; set; }
	public decimal AverageStockTransferRate { get; set; }

	public decimal LastBillRate { get; set; }
	public decimal AverageBillRate { get; set; }

	public decimal LastAdjustmentRate { get; set; }
	public decimal AverageAdjustmentRate { get; set; }
}
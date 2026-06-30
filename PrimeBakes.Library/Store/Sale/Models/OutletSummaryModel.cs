namespace PrimeBakes.Library.Store.Sale.Models;

public class OutletSummaryModel
{
	public int LocationId { get; set; }
	public string LocationName { get; set; }

	public decimal Purchase { get; set; }
	public decimal PurchaseReturn { get; set; }
	public decimal KitchenIssue { get; set; }
	public decimal KitchenProduction { get; set; }
	public decimal Sale { get; set; }
	public decimal SaleReturn { get; set; }

	// Payment mix (populated during calculation)
	public decimal Cash { get; set; }
	public decimal Card { get; set; }
	public decimal UPI { get; set; }
	public decimal Credit { get; set; }

	// Volume (populated during calculation from the sale-side transactions)
	public int TransactionCount { get; set; }
	public decimal UnitsSold { get; set; }

	// Share of the total net sale across all outlets (needs the grand total, so set during calculation)
	public decimal ContributionPercent { get; set; }

	// Derived analytics (computed from the values above)
	public decimal NetPurchase => Purchase - PurchaseReturn;
	public decimal NetProduction => KitchenProduction - KitchenIssue;
	public decimal NetSale => Sale - SaleReturn;

	public decimal GrossProfit => NetSale - NetPurchase;
	public decimal MarginPercent => NetSale == 0 ? 0 : Math.Round(GrossProfit / NetSale * 100, 2);
	public decimal AverageSaleValue => TransactionCount == 0 ? 0 : Math.Round(NetSale / TransactionCount, 2);

	public decimal PurchaseReturnPercent => Purchase == 0 ? 0 : Math.Round(PurchaseReturn / Purchase * 100, 2);
	public decimal KitchenProductionPercent => KitchenProduction == 0 ? 0 : Math.Round(KitchenIssue / KitchenProduction * 100, 2);
	public decimal SaleReturnPercent => Sale == 0 ? 0 : Math.Round(SaleReturn / Sale * 100, 2);
}
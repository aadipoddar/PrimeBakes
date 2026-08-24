namespace PrimeBakes.Models.Inventory.Summary;

public class KitchenSummaryModel
{
	public int KitchenId { get; set; }
	public string KitchenName { get; set; }

	public decimal KitchenIssue { get; set; }
	public decimal KitchenIssueReturn { get; set; }
	public decimal KitchenProduction { get; set; }
	public decimal KitchenProductionReturn { get; set; }

	public int TransactionCount { get; set; }
	public decimal UnitsProduced { get; set; }

	// Share of the total net production across all kitchens (needs the grand total, so set during calculation)
	public decimal ContributionPercent { get; set; }

	// Derived analytics (computed from the values above)
	// Material actually consumed by the kitchen: what was issued, less what came back
	public decimal NetKitchenIssue => KitchenIssue - KitchenIssueReturn;
	// Goods the kitchen actually produced: what was recorded, less what was reversed
	public decimal NetKitchenProduction => KitchenProduction - KitchenProductionReturn;
	public decimal NetProduction => NetKitchenProduction - NetKitchenIssue;
	public decimal AverageProductionValue => TransactionCount == 0 ? 0 : Math.Round(NetProduction / TransactionCount, 2);
	public decimal KitchenProductionPercent => NetKitchenProduction == 0 ? 0 : Math.Round(NetKitchenIssue / NetKitchenProduction * 100, 2);
}
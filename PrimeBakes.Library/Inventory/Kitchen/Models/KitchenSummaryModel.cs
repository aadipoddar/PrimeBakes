namespace PrimeBakes.Library.Inventory.Kitchen.Models;

public class KitchenSummaryModel
{
	public int KitchenId { get; set; }
	public string KitchenName { get; set; }

	public decimal KitchenIssue { get; set; }
	public decimal KitchenIssueReturn { get; set; }
	public decimal KitchenProduction { get; set; }

	public int TransactionCount { get; set; }
	public decimal UnitsProduced { get; set; }

	// Share of the total net production across all kitchens (needs the grand total, so set during calculation)
	public decimal ContributionPercent { get; set; }

	// Derived analytics (computed from the values above)
	// Material actually consumed by the kitchen: what was issued, less what came back
	public decimal NetKitchenIssue => KitchenIssue - KitchenIssueReturn;
	public decimal NetProduction => KitchenProduction - NetKitchenIssue;
	public decimal AverageProductionValue => TransactionCount == 0 ? 0 : Math.Round(NetProduction / TransactionCount, 2);
	public decimal KitchenProductionPercent => KitchenProduction == 0 ? 0 : Math.Round(NetKitchenIssue / KitchenProduction * 100, 2);
}
namespace PrimeBakes.Library.Inventory.Kitchen.Models;

public class KitchenSummaryModel
{
	public int KitchenId { get; set; }
	public string KitchenName { get; set; }

	public decimal KitchenIssue { get; set; }
	public decimal KitchenProduction { get; set; }

	public int TransactionCount { get; set; }
	public decimal UnitsProduced { get; set; }

	// Share of the total net production across all kitchens (needs the grand total, so set during calculation)
	public decimal ContributionPercent { get; set; }

	// Derived analytics (computed from the values above)
	public decimal NetProduction => KitchenProduction - KitchenIssue;
	public decimal AverageProductionValue => TransactionCount == 0 ? 0 : Math.Round(NetProduction / TransactionCount, 2);
	public decimal KitchenProductionPercent => KitchenProduction == 0 ? 0 : Math.Round(KitchenIssue / KitchenProduction * 100, 2);
}
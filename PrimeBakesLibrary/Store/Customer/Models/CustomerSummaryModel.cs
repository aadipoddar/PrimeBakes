namespace PrimeBakesLibrary.Store.Customer.Models;

public class CustomerSummaryModel
{
	public int CustomerId { get; set; }
	public string Name { get; set; }
	public string Number { get; set; }

	public int SaleCount { get; set; }
	public int BillCount { get; set; }
	public int ReturnCount { get; set; }

	public decimal SaleAmount { get; set; }
	public decimal BillAmount { get; set; }
	public decimal ReturnAmount { get; set; }

	public decimal TotalQuantity { get; set; }

	public decimal Cash { get; set; }
	public decimal Card { get; set; }
	public decimal UPI { get; set; }
	public decimal Credit { get; set; }

	public DateTime? FirstPurchase { get; set; }
	public DateTime? LastPurchase { get; set; }

	// Whole days between the report's reference date and the last visit (set during calculation)
	public int DaysSinceLastVisit { get; set; }

	// Share of the total net business across all customers (needs the grand total, so set during calculation)
	public decimal ContributionPercent { get; set; }

	public int TotalTransactions => SaleCount + BillCount + ReturnCount;
	public int PurchaseCount => SaleCount + BillCount;
	public decimal GrossBusiness => SaleAmount + BillAmount;
	public decimal NetBusiness => GrossBusiness - ReturnAmount;
	public decimal AverageOrderValue => PurchaseCount == 0 ? 0 : Math.Round(NetBusiness / PurchaseCount, 2);
	public decimal ReturnPercent => GrossBusiness == 0 ? 0 : Math.Round(ReturnAmount / GrossBusiness * 100, 2);
}

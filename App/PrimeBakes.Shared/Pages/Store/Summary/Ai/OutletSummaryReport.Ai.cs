using PrimeBakes.Models.Store.Summary;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Summary;

public partial class OutletSummaryReport
{
	private const int MainOutletLocationId = 1;

	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the OUTLET SUMMARY REPORT: one row per location with its sales, returns and payment modes, plus purchasing and kitchen figures for the main outlet.
		REPORT DATA has the period and company filter, then PRECOMPUTED TOTALS, a MAIN OUTLET block and an OUTLET TABLE (CSV).
		The outlet table covers every location except the main outlet. Its columns:
		outlet, category (Coco, Fofo or Other), sale (sales, stock transfers and restaurant bills made at that outlet), sale_return (returns booked at that outlet),
		net_sale (sale less sale_return), transactions, units, avg_bill (net sale divided by transactions), cash, upi, card, credit,
		from_main (stock supplied to the outlet by the main outlet: sales to its ledger plus stock transfers to it),
		returned_to_main (returns booked against the outlet's ledger), share (percent of all outlets' net sale, main outlet left out).

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It buys raw material (purchase), issues it to the kitchen (kitchen issue), makes finished products (kitchen production) and sells the products to the other outlets on credit. That is internal supply, not customer sales. So its credit sales and its lack of cash, card or UPI are normal and not a concern.
		- Kitchen issue returns and kitchen production returns are damages.
		- The main outlet's supply to outlets shows up again as "from_main" at the outlets. Never add the two together.
		- The other outlets sell to customers for cash, UPI or card. Coco outlets are company owned and managed, Fofo outlets are franchise owned, and Other is a different kind of outlet.
		- Outlet sales reach the software in three ways: some outlets bill live, some have the whole day's sales entered at the end of the day, and for some only the remaining amount is handed over to the main outlet. So an outlet with low or no recorded sales is NOT necessarily quiet or weak. Never call an outlet inactive or underperforming; say "recorded sales" instead.
		- Purchases are stock bought, not the cost of goods sold. This report has no profit figure, so never present sales minus purchases as profit.
		- For questions about customer sales, comparing outlets or the payment split, use the outlet figures only and say that the main outlet is left out.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) outlet net sale, transactions and units for the period, leaving out the main outlet; " +
		"(2) the outlet with the highest net sale and the one with the lowest among outlets that have recorded sales, with their amounts; " +
		"(3) the cash, UPI and card split of outlet sales as percentages; " +
		"(4) the main outlet's purchase, its supply to outlets, and the kitchen damage percentages; " +
		"(5) one thing that needs attention from this data, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which outlet sold the most?",
		"Compare Coco and Fofo outlets",
		"Which outlets have the highest average bill?",
		"How much damage is there in the kitchen?"
	];

	// ---------- AI wiring ----------
	private bool _showAi = false;
	private DateTime _aiToday = DateTime.Now.Date;

	private async Task ToggleAi()
	{
		_showAi = !_showAi;
		if (_showAi)
			_aiToday = (await CommonData.LoadCurrentDateTime()).Date;
	}

	// Text version of what is on screen (_outletSummaries already has every filter applied).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";

		string Category(OutletSummaryModel o)
		{
			var location = _locations.FirstOrDefault(l => l.Id == o.LocationId);
			return location is null ? "Other" : location.COCO ? "Coco" : location.FOFO ? "Fofo" : "Other";
		}

		string Avg(OutletSummaryModel o) => o.TransactionCount == 0 ? "0" : N(D(o.NetSale) / o.TransactionCount);

		var head = _outletSummaries.FirstOrDefault(o => o.LocationId == MainOutletLocationId);
		var outlets = _outletSummaries.Where(o => o.LocationId != MainOutletLocationId).OrderByDescending(o => o.NetSale).ToList();

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine("Deleted transactions are excluded.");

		var outletNet = D(outlets.Sum(o => o.NetSale));
		var cash = D(outlets.Sum(o => o.Cash));
		var upi = D(outlets.Sum(o => o.UPI));
		var card = D(outlets.Sum(o => o.Card));
		var credit = D(outlets.Sum(o => o.Credit));
		var paid = cash + upi + card + credit;

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Outlets (all locations except the main outlet): outlets={outlets.Count}, net_sale={N(outletNet)}, sale={N(outlets.Sum(o => o.Sale))}, " +
			$"sale_return={N(outlets.Sum(o => o.SaleReturn))}, transactions={outlets.Sum(o => o.TransactionCount)}, units={N(outlets.Sum(o => o.UnitsSold), "0.##")}");
		text.AppendLine($"Outlet payment split: cash={N(cash)} ({Pct(cash, paid)}), upi={N(upi)} ({Pct(upi, paid)}), card={N(card)} ({Pct(card, paid)}), credit={N(credit)} ({Pct(credit, paid)})");

		text.AppendLine("Outlets by category: " + string.Join("; ", outlets
			.GroupBy(o => Category(o))
			.Select(g => new { Name = g.Key, Count = g.Count(), Net = D(g.Sum(o => o.NetSale)), Transactions = g.Sum(o => o.TransactionCount) })
			.OrderByDescending(x => x.Net)
			.Select(x => $"{x.Name}: {x.Count} outlets, net_sale={N(x.Net)} ({Pct(x.Net, outletNet)}), {x.Transactions} transactions")));

		text.AppendLine("Highest average bill (outlets with recorded sales): " + string.Join("; ", outlets
			.Where(o => o.TransactionCount > 0)
			.OrderByDescending(o => D(o.NetSale) / o.TransactionCount).Take(5)
			.Select(o => $"{Clean(o.LocationName)}={Avg(o)}")));

		text.AppendLine("Outlets with no recorded sales in the period: " + string.Join("; ", outlets
			.Where(o => o.TransactionCount == 0)
			.Select(o => Clean(o.LocationName))));

		if (head is not null)
		{
			text.AppendLine();
			text.AppendLine($"MAIN OUTLET ({Clean(head.LocationName)})");
			text.AppendLine($"purchase={N(head.Purchase)}, purchase_return={N(head.PurchaseReturn)}, kitchen_issue={N(head.KitchenIssue)}, kitchen_issue_return={N(head.KitchenIssueReturn)}, " +
				$"kitchen_production={N(head.KitchenProduction)}, kitchen_production_return={N(head.KitchenProductionReturn)}");
			text.AppendLine($"sale (its sales, stock transfers and bills, mostly supply to outlets)={N(head.Sale)}, sale_return={N(head.SaleReturn)}, net_sale={N(head.NetSale)}, " +
				$"transactions={head.TransactionCount}, units={N(head.UnitsSold, "0.##")}");
			text.AppendLine($"Kitchen damage: issue returns are {Pct(D(head.KitchenIssueReturn), D(head.KitchenIssue))} of kitchen issue; " +
				$"production returns are {Pct(D(head.KitchenProductionReturn), D(head.KitchenProduction))} of kitchen production.");
		}

		text.AppendLine();
		text.AppendLine("OUTLET TABLE (CSV)");
		text.AppendLine("outlet,category,sale,sale_return,net_sale,transactions,units,avg_bill,cash,upi,card,credit,from_main,returned_to_main,share");

		foreach (var o in outlets)
			text.AppendLine(string.Join(',',
				Clean(o.LocationName),
				Category(o),
				N(o.Sale),
				N(o.SaleReturn),
				N(o.NetSale),
				o.TransactionCount.ToString(),
				N(o.UnitsSold, "0.##"),
				Avg(o),
				N(o.Cash),
				N(o.UPI),
				N(o.Card),
				N(o.Credit),
				N(o.Purchase),
				N(o.PurchaseReturn),
				Pct(D(o.NetSale), outletNet).TrimEnd('%')));

		return text.ToString();
	}
}

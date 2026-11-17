using PrimeBakes.Models.Store.Summary;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Summary;

public partial class CustomerSummaryReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the CUSTOMER SUMMARY REPORT: one row per customer who has sales, restaurant bills or returns in the period.
		Sales and bills that are not linked to a customer are NOT in this data. So these totals are not the whole business: never quote them as total sales.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and lists, then a CUSTOMER TABLE (CSV) with the top 50 customers by net business.
		CSV columns: customer, visits (sales plus bills), gross (sales plus bills), returns, net (gross less returns), units, days_since_last_visit, share (percent of all customers' net business).
		Days since last visit are counted up to the reference date given in the data, not up to the end of the period.
		Customers are identified by name only. There are no contact details in the data.

		HOW THE BUSINESS WORKS
		- Returns are either damaged stock or customers returning items. The data does not say which, so call them "returns" and never assume a cause.
		- A customer who has not visited for a while is not necessarily lost. Say "has not visited for N days" and nothing stronger.
		- First and last visits only cover the chosen period. A customer first seen in this period may have bought before it.
		- Copy customer names exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the number of customers, their visits, net business and returns for the period, saying these are customer-linked sales only; " +
		"(2) the top three customers by net business, with their amounts and shares; " +
		"(3) how concentrated the business is: the share of net business from the top five customers; " +
		"(4) valuable customers who have not visited recently, using the lapsed list, with the top three and their days since last visit; " +
		"(5) one thing that needs attention from this data, such as a customer with a high returns percentage, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Who are my top customers?",
		"Who has stopped visiting?",
		"Which customers return the most?",
		"How do customers pay?"
	];

	// ---------- AI wiring ----------
	private bool _showAi = false;

	private void ToggleAi() => _showAi = !_showAi;

	// Text version of what is on screen (_customerSummaries already has every filter applied).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";
		static string LastVisit(CustomerSummaryModel c) => c.LastPurchase is null ? "no purchase" : $"{c.DaysSinceLastVisit} days ago";

		var all = _customerSummaries.OrderByDescending(c => c.NetBusiness).ToList();

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		text.AppendLine($"Reference date for days since last visit: {_referenceDate:dd-MMM-yyyy}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Location filter: {(_selectedLocation?.Id > 0 ? _selectedLocation.Name : "All")}");
		text.AppendLine("Deleted transactions are excluded. Sales and bills without a customer are not in this data.");

		var gross = D(all.Sum(c => c.GrossBusiness));
		var returns = D(all.Sum(c => c.ReturnAmount));
		var net = D(all.Sum(c => c.NetBusiness));

		var cash = D(all.Sum(c => c.Cash));
		var upi = D(all.Sum(c => c.UPI));
		var card = D(all.Sum(c => c.Card));
		var credit = D(all.Sum(c => c.Credit));
		var paid = cash + upi + card + credit;

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All customers: customers={all.Count}, visits={all.Sum(c => c.PurchaseCount)}, units={N(all.Sum(c => c.TotalQuantity), "0.##")}, " +
			$"gross={N(gross)}, returns={N(returns)} ({Pct(returns, gross)} of gross), net={N(net)}");
		text.AppendLine($"Customers with only one visit: {all.Count(c => c.PurchaseCount == 1)}");
		text.AppendLine($"Payment split of sales and bills: cash={N(cash)} ({Pct(cash, paid)}), upi={N(upi)} ({Pct(upi, paid)}), card={N(card)} ({Pct(card, paid)}), credit={N(credit)} ({Pct(credit, paid)})");
		text.AppendLine($"Top 5 customers together: {N(all.Take(5).Sum(c => c.ContributionPercent), "0.##")}% of net business");

		text.AppendLine("Top 15 customers by net business: " + string.Join("; ", all.Take(15)
			.Select(c => $"{Clean(c.Name)}: net {N(c.NetBusiness)}, {c.PurchaseCount} visits, share {N(c.ContributionPercent, "0.##")}%, last visit {LastVisit(c)}")));

		text.AppendLine("Lapsed valuable customers (no visit for 30 days or more), by net business: " + string.Join("; ", all
			.Where(c => c.LastPurchase is not null && c.DaysSinceLastVisit >= 30)
			.Take(10)
			.Select(c => $"{Clean(c.Name)}: net {N(c.NetBusiness)}, {c.DaysSinceLastVisit} days since last visit")));

		text.AppendLine("Customers with the highest returns: " + string.Join("; ", all
			.Where(c => c.ReturnAmount > 0)
			.OrderByDescending(c => c.ReturnAmount).Take(5)
			.Select(c => $"{Clean(c.Name)}: returns {N(c.ReturnAmount)} ({(c.GrossBusiness == 0 ? "0" : N(c.ReturnAmount / c.GrossBusiness * 100, "0.#"))}% of their gross)")));

		text.AppendLine();
		text.AppendLine("CUSTOMER TABLE (CSV, top 50 by net business)");
		text.AppendLine("customer,visits,gross,returns,net,units,days_since_last_visit,share");

		foreach (var c in all.Take(50))
			text.AppendLine(string.Join(',',
				Clean(c.Name),
				c.PurchaseCount.ToString(),
				N(c.GrossBusiness),
				N(c.ReturnAmount),
				N(c.NetBusiness),
				N(c.TotalQuantity, "0.##"),
				c.LastPurchase is null ? string.Empty : c.DaysSinceLastVisit.ToString(),
				N(c.ContributionPercent, "0.##")));

		return text.ToString();
	}
}

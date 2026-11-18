using PrimeBakes.Models.Inventory.Summary;

using Syncfusion.Blazor.Data;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Summary;

public partial class KitchenSummaryReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the KITCHEN SUMMARY REPORT: for each kitchen, what was issued to it and what it produced over one period. REPORT DATA has the period and filter, then PRECOMPUTED TOTALS, then per kitchen lines and a KITCHEN TABLE (CSV).
		KITCHEN TABLE columns: kitchen, issue (rupee value of raw material issued to the kitchen), issue_return (rupee value of raw material that came back), production (rupee value of finished products made), production_return (rupee value of finished products that came back), units_produced (net units of finished products), transactions (number of issues, issue returns, productions and production returns together).
		Values are in rupees. Net issue is issue less issue_return. Net production is production less production_return. Only transactions that are not deleted are counted.
		This report has totals per kitchen for the whole period only. It has no split by month, day, product or raw material. If asked for those, say that this report does not have them and that the item or monthly reports do.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) buys raw material and issues it to the kitchen (kitchen issue), and the kitchen makes the finished products (kitchen production). A kitchen issue return and a kitchen production return are damage coming back. Never guess the cause of a return.
		- Issue is a raw material cost. Production is what the finished products are valued at when recorded. Neither is sales. Never call them revenue or sales.
		- Net production value as a percentage of net issue value is NOT a yield, efficiency or profit figure: raw material and finished products are valued differently, and issues and production in a period do not line up one-to-one. Quote the percentage only if asked and say what it is.
		- A kitchen with little or no activity just has "no recorded activity"; it is not necessarily idle.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total net issue value and total net production value for the period; " +
		"(2) the kitchen with the highest net production value and its share of the total; if only one kitchen has activity, say so briefly; " +
		"(3) the issue return rate (issue_return as a percentage of issue) and the production return rate (production_return as a percentage of production), overall; " +
		"(4) total units produced and the number of transactions; " +
		"(5) one thing that needs attention, such as a kitchen with issues but no production, or a much higher return rate than the others, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which kitchen produced the most?",
		"How high are the return rates?",
		"Compare issue and production by kitchen",
		"Which kitchens have no production?"
	];

	private bool _showAi = false;
	private DateTime _aiToday = DateTime.Now.Date;

	private async Task ToggleAi()
	{
		_showAi = !_showAi;
		if (_showAi)
			_aiToday = (await CommonData.LoadCurrentDateTime()).Date;
	}

	// Text version of what is on screen (_kitchenSummaries has one row per kitchen with every filter applied).
	// The totals and rates are computed here from the raw amounts, because the model is unreliable at arithmetic.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static string Pct(double part, double whole) => whole == 0 ? "n/a" : $"{(part * 100 / whole).ToString("0.#", CultureInfo.InvariantCulture)}%";

		var all = _kitchenSummaries;

		double Issue(KitchenSummaryModel k) => Convert.ToDouble(k.KitchenIssue);
		double IssueReturn(KitchenSummaryModel k) => Convert.ToDouble(k.KitchenIssueReturn);
		double Production(KitchenSummaryModel k) => Convert.ToDouble(k.KitchenProduction);
		double ProductionReturn(KitchenSummaryModel k) => Convert.ToDouble(k.KitchenProductionReturn);

		var totalIssue = all.Sum(Issue);
		var totalIssueReturn = all.Sum(IssueReturn);
		var totalProduction = all.Sum(Production);
		var totalProductionReturn = all.Sum(ProductionReturn);
		var netIssue = totalIssue - totalIssueReturn;
		var netProduction = totalProduction - totalProductionReturn;

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Kitchens listed: {all.Count}; with any activity: {all.Count(k => k.TransactionCount > 0)}");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Issue: issue={N(totalIssue)}, issue_return={N(totalIssueReturn)}, net issue={N(netIssue)}, issue return rate={Pct(totalIssueReturn, totalIssue)}");
		text.AppendLine($"Production: production={N(totalProduction)}, production_return={N(totalProductionReturn)}, net production={N(netProduction)}, production return rate={Pct(totalProductionReturn, totalProduction)}");
		text.AppendLine($"Units produced (net)={N(all.Sum(k => k.UnitsProduced), "0.##")}, transactions={all.Sum(k => k.TransactionCount)}");
		text.AppendLine($"Net production value as a percentage of net issue value, all kitchens: {Pct(netProduction, netIssue)} (not a yield or profit figure)");

		text.AppendLine();
		text.AppendLine("Per kitchen, highest net production first:");
		foreach (var k in all.OrderByDescending(k => Production(k) - ProductionReturn(k)))
		{
			var kitchenNetIssue = Issue(k) - IssueReturn(k);
			var kitchenNetProduction = Production(k) - ProductionReturn(k);
			text.AppendLine($"- {Clean(k.KitchenName)}: net issue {N(kitchenNetIssue)} (issue return rate {Pct(IssueReturn(k), Issue(k))}), " +
				$"net production {N(kitchenNetProduction)} (share {Pct(kitchenNetProduction, netProduction)}; production return rate {Pct(ProductionReturn(k), Production(k))}), " +
				$"{N(k.UnitsProduced, "0.##")} units, {k.TransactionCount} transactions, net production as % of net issue {Pct(kitchenNetProduction, kitchenNetIssue)}");
		}

		text.AppendLine();
		text.AppendLine("KITCHEN TABLE (CSV)");
		text.AppendLine("kitchen,issue,issue_return,production,production_return,units_produced,transactions");

		foreach (var k in all.OrderBy(k => k.KitchenName))
			text.AppendLine(string.Join(',',
				Clean(k.KitchenName),
				N(k.KitchenIssue),
				N(k.KitchenIssueReturn),
				N(k.KitchenProduction),
				N(k.KitchenProductionReturn),
				N(k.UnitsProduced, "0.##"),
				k.TransactionCount.ToString()));

		return text.ToString();
	}
}

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen.KitchenProduction.Reports;

public partial class KitchenProductionReturnReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the KITCHEN PRODUCTION RETURN REPORT: finished products coming back from the kitchens. REPORT DATA has the period and filters, then PRECOMPUTED TOTALS, then a "Granularity" line and a CSV table.
		CSV columns: period, kitchen, returns (number of returns, can be blank), qty (units of finished products returned), value (rupee value at the rates recorded).
		Granularity says whether each row is one hour, one day, one month or the whole period for a kitchen.
		"item lines" means the number of product lines on the returns, not a quantity. Use qty for how many units came back.
		This report has returns only. It does not have the production they came from, so do not give a return rate or a percentage of production.
		In the whole-period summary view there is one row per kitchen and no counts or dates. If asked for those, say that view does not have them.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) buys raw material, issues it to the kitchen, and the kitchen makes the finished products. A kitchen production return is finished products coming back, which the business treats as damage. Never guess the cause of a return.
		- Value is what the products are valued at when recorded, not sales. Never call it revenue or sales.
		- A kitchen with few or no returns just has "no recorded returns"; that is not a problem.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) number of returns, total units and total value (if the data is the summary view, leave out the number and say it is not available); " +
		"(2) the kitchen with the most units returned and the one with the fewest; if only one kitchen is in the data, say so briefly; " +
		"(3) the busiest day by units returned and anything unusual in the daily pattern; if the period is a single day or the summary view, say that briefly; " +
		"(4) the largest single return with its transaction number, units, value, kitchen and date (if the data is the summary view, say it is not available); " +
		"(5) one thing that needs attention, such as a sharp rise in recent days or one kitchen returning far more than the others, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which kitchen had the most returns?",
		"Show the largest returns",
		"What are the busiest days?",
		"Show the value by kitchen"
	];

	private bool _showAi = false;
	private DateTime _aiToday = DateTime.Now.Date;

	private async Task ToggleAi()
	{
		_showAi = !_showAi;
		if (_showAi)
			_aiToday = (await CommonData.LoadCurrentDateTime()).Date;
	}

	// Text version of what is on screen (_transactionOverviews already has every filter applied).
	// The totals are computed here, because the model is unreliable at adding up hundreds of rows.
	// The CSV is aggregated more coarsely the longer the period, so a long period stays small.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');

		var all = _transactionOverviews;

		var days = (_toDate.Date - _fromDate.Date).Days + 1;
		var (grain, format) = days switch
		{
			<= 2 => ("hour", "yyyy-MM-dd HH:00"),
			<= 92 => ("day", "yyyy-MM-dd"),
			_ => ("month", "yyyy-MM")
		};

		// The summary view has no dates and is already one row per kitchen
		if (_showSummary) grain = "whole period";

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Kitchen filter: {(_selectedKitchen?.Id > 0 ? _selectedKitchen.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		if (_showSummary)
			text.AppendLine("This is the SUMMARY view: one row per kitchen for the whole period, so there is no return count and no split by day.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All returns: {(_showSummary ? string.Empty : $"returns={all.Count}, ")}item lines={N(all.Sum(t => t.TotalItems))}, " +
			$"qty={N(all.Sum(t => t.TotalQuantity), "0.##")}, value={N(all.Sum(t => t.TotalAmount))}");

		text.AppendLine("Total by kitchen, most units first: " + string.Join("; ", all
			.GroupBy(t => t.KitchenName)
			.Select(g => (Name: g.Key, Rows: g.ToList()))
			.OrderByDescending(x => x.Rows.Sum(t => t.TotalQuantity))
			.Select(x => $"{Clean(x.Name)}={N(x.Rows.Sum(t => t.TotalQuantity), "0.##")} units / {N(x.Rows.Sum(t => t.TotalAmount))}" +
				(_showSummary ? string.Empty : $" ({x.Rows.Count} returns)"))));

		if (!_showSummary)
		{
			text.AppendLine("Largest 5 returns by value: " + string.Join("; ", all
				.OrderByDescending(t => t.TotalAmount).Take(5)
				.Select(t => $"{Clean(t.TransactionNo)}={N(t.TotalAmount)} / {N(t.TotalQuantity, "0.##")} units ({Clean(t.KitchenName)}, {t.TransactionDateTime:dd-MMM-yyyy})")));

			if (days > 1)
				text.AppendLine("Top 5 days by units: " + string.Join("; ", all
					.GroupBy(t => t.TransactionDateTime.Date)
					.Select(g => (Day: g.Key, Qty: g.Sum(t => t.TotalQuantity), Total: g.Sum(t => t.TotalAmount)))
					.OrderByDescending(x => x.Qty).Take(5)
					.Select(x => $"{x.Day:dd-MMM-yyyy}={N(x.Qty, "0.##")} units / {N(x.Total)}")));
		}

		text.AppendLine();
		text.AppendLine($"Granularity: one row per {grain} per kitchen");
		text.AppendLine("period,kitchen,returns,qty,value");

		var rows = all
			.GroupBy(t => (Period: _showSummary ? "all" : t.TransactionDateTime.ToString(format), Kitchen: t.KitchenName))
			.OrderBy(g => g.Key.Period).ThenBy(g => g.Key.Kitchen);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				g.Key.Period,
				Clean(g.Key.Kitchen),
				_showSummary ? string.Empty : g.Count().ToString(),   // summary rows are already totals, so a count would mislead
				N(g.Sum(t => t.TotalQuantity), "0.##"),
				N(g.Sum(t => t.TotalAmount))));

		return text.ToString();
	}
}

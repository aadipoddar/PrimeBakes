using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen.KitchenIssue.Reports;

public partial class KitchenIssueReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the KITCHEN ISSUE REPORT: raw material issued from the main store to the kitchens. REPORT DATA has the period and filters, then PRECOMPUTED TOTALS, then a "Granularity" line and a CSV table.
		CSV columns: period, kitchen, issues (number of issues, can be blank), returns (number of returns, can be blank), value (rupee value of the raw material; net of returns when returns are included).
		Granularity says whether each row is one hour, one day, one month or the whole period for a kitchen.
		Quantities are deliberately not given: raw materials are measured in different units (kg, litres, pieces), so quantities cannot be added up. Talk about rupee value only. "item lines" means the number of raw material lines on the issues, not a quantity.
		Negative values are kitchen issue returns. Returns are present only when the data says they are included; if it says returns are not included, say so and give no returns figure.
		In the whole-period summary view there is one row per kitchen and no counts or dates. If asked for those, say that view does not have them.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) buys raw material and issues it to the kitchen, where the finished products are made. A kitchen issue is raw material going to a kitchen. A kitchen issue return is raw material coming back from the kitchen, which the business treats as damage. Never guess the cause of a return.
		- Issue value is the cost of raw material, not sales. Never call it revenue or sales.
		- A kitchen with low or no issues is NOT necessarily idle. Say "recorded issues".
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) number of issues and their total value (if the data is the summary view, leave out the number and say it is not available); " +
		"(2) returns: number and value (say they are not included if the data says so); " +
		"(3) the kitchen with the highest value and the one with the lowest; if only one kitchen is in the data, say so briefly; " +
		"(4) the busiest day by value and anything unusual in the daily pattern; if the period is a single day or the summary view, say that briefly; " +
		"(5) one thing that needs attention, such as high returns or a sharp fall in recent days against earlier days, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which kitchen got the most raw material?",
		"How much were the returns?",
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
		static bool IsReturn(KitchenIssueOverviewModel t) => t.TotalAmount < 0 || t.TotalQuantity < 0;

		var all = _transactionOverviews;

		// The summary view is already one row per kitchen with returns netted in, so issues and returns cannot be told apart there
		var issueRows = _showSummary ? all : all.Where(t => !IsReturn(t)).ToList();
		var returnRows = _showSummary ? new List<KitchenIssueOverviewModel>() : all.Where(IsReturn).ToList();

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

		text.AppendLine(!_showTransactionReturns ? "Kitchen issue returns: NOT included in this data, so there is no returns figure."
			: _showSummary ? "Kitchen issue returns: included, netted into the values."
			: $"Kitchen issue returns: included as negative rows; {returnRows.Count} returns worth {N(-returnRows.Sum(t => t.TotalAmount))} in total.");

		if (_showSummary)
			text.AppendLine("This is the SUMMARY view: one row per kitchen for the whole period, so there is no issue count and no split by day.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Issues: {(_showSummary ? string.Empty : $"issues={issueRows.Count}, ")}item lines={N(issueRows.Sum(t => t.TotalItems))}, value={N(issueRows.Sum(t => t.TotalAmount))}");
		if (_showTransactionReturns && !_showSummary)
			text.AppendLine($"Net of returns: value={N(all.Sum(t => t.TotalAmount))}");

		text.AppendLine($"Value by kitchen, highest first{(_showTransactionReturns ? " (net of returns)" : string.Empty)}: " + string.Join("; ", all
			.GroupBy(t => t.KitchenName)
			.Select(g => (Name: g.Key, Rows: g.ToList()))
			.OrderByDescending(x => x.Rows.Sum(t => t.TotalAmount))
			.Select(x => $"{Clean(x.Name)}={N(x.Rows.Sum(t => t.TotalAmount))}" +
				(_showSummary ? string.Empty : $" ({x.Rows.Count(t => !IsReturn(t))} issues, {x.Rows.Count(IsReturn)} returns)"))));

		if (!_showSummary && days > 1)
			text.AppendLine("Top 5 days by value: " + string.Join("; ", all
				.GroupBy(t => t.TransactionDateTime.Date)
				.Select(g => (Day: g.Key, Total: g.Sum(t => t.TotalAmount)))
				.OrderByDescending(x => x.Total).Take(5)
				.Select(x => $"{x.Day:dd-MMM-yyyy}={N(x.Total)}")));

		text.AppendLine();
		text.AppendLine($"Granularity: one row per {grain} per kitchen");
		text.AppendLine("period,kitchen,issues,returns,value");

		var rows = all
			.GroupBy(t => (Period: _showSummary ? "all" : t.TransactionDateTime.ToString(format), Kitchen: t.KitchenName))
			.OrderBy(g => g.Key.Period).ThenBy(g => g.Key.Kitchen);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				g.Key.Period,
				Clean(g.Key.Kitchen),
				_showSummary ? string.Empty : g.Count(t => !IsReturn(t)).ToString(),   // summary rows are already totals, so a count would mislead
				_showSummary ? string.Empty : g.Count(IsReturn).ToString(),
				N(g.Sum(t => t.TotalAmount))));

		return text.ToString();
	}
}

using PrimeBakes.Models.Store.Sale;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Sale.Reports;

public partial class SaleReturnReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the SALE RETURN REPORT. It lists returns only; there are no sales in this data. So never work out a return rate and never compare returns with sales.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS, then a "Granularity" line and a CSV table.
		CSV columns in the normal view: period, location, returns (number of return bills), units, total (final return amount), discount, cash, upi, card, credit (how the amount was settled).
		In the SUMMARY view the table is one row per party for the whole period, with no location or day split.
		All amounts are positive: they are the value of what was returned.

		HOW THE BUSINESS WORKS
		- Returns are either damaged stock sent back from outlets to the main outlet, or customers returning items to outlets. The data does not say which, so call them "returns" and never assume a cause.
		- Without sales to compare against, do not call a return amount high or low. Compare only locations, parties and days within this data.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the number of returns, units and total return amount for the period; " +
		"(2) the location with the highest return total and the one with the lowest, among locations that have returns; if only one location is in the data, say so briefly; " +
		"(3) how the returns were settled, as the cash, UPI, card and credit split in percentages of the total; " +
		"(4) the day (or month) with the highest return total and anything unusual in the pattern; " +
		"(5) one thing that needs attention from this data, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which location returns the most?",
		"Which parties have the most returns?",
		"How were the returns settled?",
		"Which days had the highest returns?"
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

	// Text version of what is on screen (_transactionOverviews already has every filter applied).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');

		string Totals(List<SaleReturnOverviewModel> rows) =>
			$"{(_showSummary ? string.Empty : $"returns={rows.Count}, ")}units={N(rows.Sum(t => t.TotalQuantity), "0.##")}, total={N(rows.Sum(t => t.TotalAmount))}, " +
			$"discount={N(rows.Sum(t => t.DiscountAmount))}, cash={N(rows.Sum(t => t.Cash))}, upi={N(rows.Sum(t => t.UPI))}, " +
			$"card={N(rows.Sum(t => t.Card))}, credit={N(rows.Sum(t => t.Credit))}";

		var all = _transactionOverviews;

		var days = (_toDate.Date - _fromDate.Date).Days + 1;
		var (grain, format) = days switch
		{
			<= 2 => ("hour", "yyyy-MM-dd HH:00"),
			<= 92 => ("day", "yyyy-MM-dd"),
			_ => ("month", "yyyy-MM")
		};

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Location filter: {(_selectedLocation?.Id > 0 ? _selectedLocation.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Party filter: {(_selectedParty?.Id > 0 ? _selectedParty.Name : "All")}");
		text.AppendLine($"Online partner filter: {(_selectedOnlinePartner?.Id > 0 ? _selectedOnlinePartner.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");
		if (_showSummary)
			text.AppendLine("This is the SUMMARY view: one row per party for the whole period, so there is no split by location or day.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All returns: {Totals(all)}");

		if (!_showSummary)
		{
			text.AppendLine("By location, highest total first: " + string.Join("; ", all
				.GroupBy(t => t.LocationName)
				.Select(g => new { Name = g.Key, Count = g.Count(), Qty = g.Sum(t => t.TotalQuantity), Total = g.Sum(t => t.TotalAmount) })
				.OrderByDescending(x => x.Total)
				.Select(x => $"{Clean(x.Name)}={N(x.Total)} ({x.Count} returns, {N(x.Qty, "0.##")} units)")));

			text.AppendLine("Top 10 parties by total: " + string.Join("; ", all
				.GroupBy(t => t.PartyName)
				.Select(g => new { Name = g.Key, Count = g.Count(), Total = g.Sum(t => t.TotalAmount) })
				.OrderByDescending(x => x.Total).Take(10)
				.Select(x => $"{Clean(x.Name)}={N(x.Total)} ({x.Count} returns)")));

			if (days > 1)
				text.AppendLine("Top 5 days by return total: " + string.Join("; ", all
					.GroupBy(t => t.TransactionDateTime.Date)
					.Select(g => new { Day = g.Key, Total = g.Sum(t => t.TotalAmount) })
					.OrderByDescending(x => x.Total).Take(5)
					.Select(x => $"{x.Day:dd-MMM-yyyy}={N(x.Total)}")));
		}

		text.AppendLine();

		if (_showSummary)
		{
			text.AppendLine("Granularity: one row per party for the whole period");
			text.AppendLine("party,units,total,discount,cash,upi,card,credit");

			foreach (var t in all)
				text.AppendLine(string.Join(',',
					Clean(t.PartyName),
					N(t.TotalQuantity, "0.##"),
					N(t.TotalAmount),
					N(t.DiscountAmount),
					N(t.Cash),
					N(t.UPI),
					N(t.Card),
					N(t.Credit)));

			return text.ToString();
		}

		text.AppendLine($"Granularity: one row per {grain} per location");
		text.AppendLine("period,location,returns,units,total,discount,cash,upi,card,credit");

		var rows = all
			.GroupBy(t => (Period: t.TransactionDateTime.ToString(format), Location: t.LocationName))
			.OrderBy(g => g.Key.Period).ThenBy(g => g.Key.Location);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				g.Key.Period,
				Clean(g.Key.Location),
				g.Count().ToString(),
				N(g.Sum(t => t.TotalQuantity), "0.##"),
				N(g.Sum(t => t.TotalAmount)),
				N(g.Sum(t => t.DiscountAmount)),
				N(g.Sum(t => t.Cash)),
				N(g.Sum(t => t.UPI)),
				N(g.Sum(t => t.Card)),
				N(g.Sum(t => t.Credit))));

		return text.ToString();
	}
}

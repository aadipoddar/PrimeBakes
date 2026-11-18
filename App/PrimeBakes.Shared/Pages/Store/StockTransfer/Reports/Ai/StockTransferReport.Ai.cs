using PrimeBakes.Models.Store.StockTransfer;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.StockTransfer.Reports;

public partial class StockTransferReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the STOCK TRANSFER REPORT: stock moved from one location (from) to another (to), valued like a sale.
		These are internal stock movements, not customer sales.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and rankings, then a TRANSFER TABLE (CSV).
		CSV columns in the normal view: period, from, to, transfers, units, total (final amount), discount, cash, upi, card, credit (how the amount was settled).
		In the SUMMARY view the table is one row per receiving location for the whole period, with no from-location and no day split.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). Transfers from it are supply to the other locations.
		- Credit is the part settled on credit. The data has no due dates, so never call credit overdue or unpaid, and do not present it as a concern.
		- A location that received nothing in the period is not necessarily inactive. Say "no transfers in this period".
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the number of transfers, units and total value for the period; " +
		"(2) the receiving location with the highest total value and the one with the lowest, among locations that received; if only one location received, say so briefly; " +
		"(3) the busiest from-to route by total value; " +
		"(4) how the transfers were settled, as the cash, UPI, card and credit split in percentages of the total; " +
		"(5) one thing that needs attention from this data, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which location received the most?",
		"What are the busiest routes?",
		"How were transfers settled?",
		"Which days had the most transfers?"
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
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";

		string Totals(List<StockTransferOverviewModel> rows) =>
			$"{(_showSummary ? string.Empty : $"transfers={rows.Count}, ")}units={N(rows.Sum(t => t.TotalQuantity), "0.##")}, total={N(rows.Sum(t => t.TotalAmount))}, " +
			$"discount={N(rows.Sum(t => t.DiscountAmount))}, cash={N(rows.Sum(t => t.Cash))}, upi={N(rows.Sum(t => t.UPI))}, " +
			$"card={N(rows.Sum(t => t.Card))}, credit={N(rows.Sum(t => t.Credit))}";

		var all = _transactionOverviews;
		var days = (_toDate.Date - _fromDate.Date).Days + 1;
		var (grain, format) = days switch
		{
			<= 2 => ("hour", "yyyy-MM-dd HH:00"),
			<= 31 => ("day", "yyyy-MM-dd"),
			_ => ("month", "yyyy-MM")
		};

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"From-location filter: {(_selectedLocation?.Id > 0 ? _selectedLocation.Name : "All")}");
		text.AppendLine($"To-location filter: {(_selectedToLocation?.Id > 0 ? _selectedToLocation.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		var total = D(all.Sum(t => t.TotalAmount));
		var cash = D(all.Sum(t => t.Cash));
		var upi = D(all.Sum(t => t.UPI));
		var card = D(all.Sum(t => t.Card));
		var credit = D(all.Sum(t => t.Credit));
		var paid = cash + upi + card + credit;

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");

		if (_showSummary)
		{
			text.AppendLine("This is the SUMMARY view: one row per receiving location for the whole period, so there is no from-location and no day split.");
			text.AppendLine($"All receiving locations: locations={all.Count}, {Totals(all)}");
			text.AppendLine($"Settlement split: cash={Pct(cash, paid)}, upi={Pct(upi, paid)}, card={Pct(card, paid)}, credit={Pct(credit, paid)}");
			text.AppendLine();
			text.AppendLine("TRANSFER TABLE (CSV, whole period)");
			text.AppendLine("to,units,total,discount,cash,upi,card,credit");
			foreach (var t in all.OrderByDescending(t => t.TotalAmount))
				text.AppendLine(string.Join(',', Clean(t.ToLocationName), N(t.TotalQuantity, "0.##"), N(t.TotalAmount), N(t.DiscountAmount), N(t.Cash), N(t.UPI), N(t.Card), N(t.Credit)));

			return text.ToString();
		}

		text.AppendLine($"All transfers: {Totals(all)}");
		text.AppendLine($"Settlement split: cash={N(cash)} ({Pct(cash, paid)}), upi={N(upi)} ({Pct(upi, paid)}), card={N(card)} ({Pct(card, paid)}), credit={N(credit)} ({Pct(credit, paid)})");

		text.AppendLine("By receiving location, highest total first: " + string.Join("; ", all
			.GroupBy(t => t.ToLocationName)
			.Select(g => new { Name = g.Key, Count = g.Count(), Units = g.Sum(t => t.TotalQuantity), Total = g.Sum(t => t.TotalAmount) })
			.OrderByDescending(x => x.Total)
			.Select(x => $"{Clean(x.Name)}={N(x.Total)} ({x.Count} transfers, {N(x.Units, "0.##")} units)")));

		text.AppendLine("By sending location, highest total first: " + string.Join("; ", all
			.GroupBy(t => t.LocationName)
			.Select(g => new { Name = g.Key, Count = g.Count(), Total = g.Sum(t => t.TotalAmount) })
			.OrderByDescending(x => x.Total)
			.Select(x => $"{Clean(x.Name)}={N(x.Total)} ({x.Count} transfers)")));

		text.AppendLine("Top 15 routes (from to) by total: " + string.Join("; ", all
			.GroupBy(t => (From: t.LocationName, To: t.ToLocationName))
			.Select(g => new { g.Key.From, g.Key.To, Count = g.Count(), Total = g.Sum(t => t.TotalAmount) })
			.OrderByDescending(x => x.Total).Take(15)
			.Select(x => $"{Clean(x.From)} to {Clean(x.To)}={N(x.Total)} ({x.Count} transfers)")));

		if (days > 1)
			text.AppendLine("Top 5 days by total: " + string.Join("; ", all
				.GroupBy(t => t.TransactionDateTime.Date)
				.Select(g => new { Day = g.Key, Count = g.Count(), Total = g.Sum(t => t.TotalAmount) })
				.OrderByDescending(x => x.Total).Take(5)
				.Select(x => $"{x.Day:dd-MMM-yyyy}={N(x.Total)} ({x.Count} transfers)")));

		text.AppendLine();
		text.AppendLine($"TRANSFER TABLE (CSV, one row per {grain} per route)");
		text.AppendLine("period,from,to,transfers,units,total,discount,cash,upi,card,credit");

		var rows = all
			.GroupBy(t => (Period: t.TransactionDateTime.ToString(format), From: t.LocationName, To: t.ToLocationName))
			.OrderBy(g => g.Key.Period).ThenBy(g => g.Key.From).ThenBy(g => g.Key.To)
			.Take(600);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				g.Key.Period,
				Clean(g.Key.From),
				Clean(g.Key.To),
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

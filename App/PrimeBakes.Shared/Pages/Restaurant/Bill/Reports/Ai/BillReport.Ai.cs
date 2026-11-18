using PrimeBakes.Models.Restaurant.Bill;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Reports;

public partial class BillReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the BILL REPORT: restaurant bills (dine-in tables and online partner orders) for the period. All amounts are in rupees.
		A bill is "settled" when it is no longer running, and "running" while its table is still open. A running bill's amount is the bill so far, not final revenue.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and lists, then a BILL TABLE (CSV).
		CSV columns in the normal view (settled bills only): period, location, bills, people, units, total, discount, service_charge, cash, upi, card, credit.
		In the SUMMARY view the table is one row per location for the whole period, with no bill count, no hour or day split and no running or settled split; running bills are inside its totals.

		HOW THE BUSINESS WORKS
		- The restaurant outlet bills tables and keeps running bills for open tables. Bill revenue means settled bills; running bills are never added to it.
		- A running bill is an open table, not a problem by itself. Say how long it has been open and that it is still running. Running bills opened on an earlier day are worth pointing out, because the table may still need to be closed; never assume why.
		- Average bill is total divided by bills. Per person is total divided by people (covers).
		- Discount and service charge are shown separately from the total. Cash, UPI, card and credit show how settled bills were paid.
		- Online partner bills are orders that came through a delivery or ordering partner; dining area and table apply to dine-in bills.
		- Quote the precomputed lists instead of adding up rows yourself. Copy table, area and partner names exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) settled bills, people, total, average bill and amount per person for the period, and the running bills with their amount so far (say the split is not available if the data says so); " +
		"(2) the busiest hour of the day by settled bills and, if the period has several days, the busiest day by total; " +
		"(3) the dining area or table with the highest total, and the share of bills from online partners if there are any; " +
		"(4) the cash, UPI, card and credit split of settled bills as percentages, with the discount and service charge totals; " +
		"(5) one thing that needs attention from this data, such as running bills opened on an earlier day, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which hours are busiest?",
		"Which tables earn the most?",
		"Are any running bills still open?",
		"How do customers pay?"
	];

	// ---------- AI wiring ----------
	private bool _showAi = false;
	private DateTime _aiNow = DateTime.Now;

	private async Task ToggleAi()
	{
		_showAi = !_showAi;
		if (_showAi)
			_aiNow = await CommonData.LoadCurrentDateTime();
	}

	// Text version of what is on screen (_transactionOverviews already has every filter applied).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";

		string Totals(List<BillOverviewModel> rows)
		{
			var total = D(rows.Sum(t => t.TotalAmount));
			var people = D(rows.Sum(t => t.TotalPeople));
			return $"{(_showSummary ? string.Empty : $"bills={rows.Count}, ")}people={N(people)}, units={N(rows.Sum(t => t.TotalQuantity), "0.##")}, total={N(total)}" +
				(_showSummary ? string.Empty : $", average bill={(rows.Count == 0 ? "0" : N(total / rows.Count))}") +
				$", per person={(people == 0 ? "n/a" : N(total / people))}, discount={N(rows.Sum(t => t.DiscountAmount))}, service charge={N(rows.Sum(t => t.ServiceChargeAmount))}, " +
				$"cash={N(rows.Sum(t => t.Cash))}, upi={N(rows.Sum(t => t.UPI))}, card={N(rows.Sum(t => t.Card))}, credit={N(rows.Sum(t => t.Credit))}";
		}

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
		text.AppendLine($"Now is {_aiNow:dd-MMM-yyyy HH:mm}." + (_toDate.Date >= _aiNow.Date ? " The last day of the period is incomplete." : string.Empty));
		text.AppendLine($"Location filter: {(_selectedLocation?.Id > 0 ? _selectedLocation.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Customer filter: {(_selectedCustomer?.Id > 0 ? _selectedCustomer.Name : "All")}");
		text.AppendLine($"Online partner filter: {(_selectedOnlinePartner?.Id > 0 ? _selectedOnlinePartner.Name : "All")}");
		text.AppendLine($"Bills shown: {(_runningFilter == YesNoFilterOptions.Yes ? "running only" : _runningFilter == YesNoFilterOptions.No ? "settled only" : "settled and running")}. Deleted bills: {(_showDeleted ? "included" : "not included")}.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");

		if (_showSummary)
		{
			text.AppendLine("This is the SUMMARY view: one row per location for the whole period, with no bill count, no hour or day split and no running or settled split.");
			text.AppendLine($"All locations: locations={all.Count}, {Totals(all)}");
			text.AppendLine();
			text.AppendLine("BILL TABLE (CSV, whole period)");
			text.AppendLine("location,people,items,units,total,discount,service_charge,cash,upi,card,credit");
			foreach (var t in all.OrderByDescending(t => t.TotalAmount))
				text.AppendLine(string.Join(',', Clean(t.LocationName), N(t.TotalPeople), N(t.TotalItems), N(t.TotalQuantity, "0.##"), N(t.TotalAmount), N(t.DiscountAmount),
					N(t.ServiceChargeAmount), N(t.Cash), N(t.UPI), N(t.Card), N(t.Credit)));

			return text.ToString();
		}

		var settled = all.Where(t => !t.Running).ToList();
		var running = all.Where(t => t.Running).ToList();
		var settledTotal = D(settled.Sum(t => t.TotalAmount));
		var paid = D(settled.Sum(t => t.Cash)) + D(settled.Sum(t => t.UPI)) + D(settled.Sum(t => t.Card)) + D(settled.Sum(t => t.Credit));

		text.AppendLine($"Settled bills: {Totals(settled)}");
		text.AppendLine($"Settled payment split: cash={Pct(D(settled.Sum(t => t.Cash)), paid)}, upi={Pct(D(settled.Sum(t => t.UPI)), paid)}, card={Pct(D(settled.Sum(t => t.Card)), paid)}, credit={Pct(D(settled.Sum(t => t.Credit)), paid)}");
		text.AppendLine($"Running bills (open tables, not revenue): bills={running.Count}, amount so far={N(running.Sum(t => t.TotalAmount))}, opened before today={running.Count(t => t.TransactionDateTime.Date < _aiNow.Date)}");

		text.AppendLine("Running bills, oldest first: " + string.Join("; ", running
			.OrderBy(t => t.TransactionDateTime).Take(15)
			.Select(t => $"{Clean(t.TransactionNo)}: {Clean(t.DiningAreaName)} {Clean(t.DiningTableName)}, {N(t.TotalAmount)} so far, opened {t.TransactionDateTime:dd-MMM HH:mm}, open {N((_aiNow - t.TransactionDateTime).TotalHours, "0.#")} hours")));

		text.AppendLine("By dining area, highest total first: " + string.Join("; ", settled
			.GroupBy(t => string.IsNullOrWhiteSpace(t.DiningAreaName) ? "No dining area" : t.DiningAreaName)
			.Select(g => new { Name = g.Key, Bills = g.Count(), Total = D(g.Sum(t => t.TotalAmount)), People = D(g.Sum(t => t.TotalPeople)) })
			.OrderByDescending(x => x.Total)
			.Select(x => $"{Clean(x.Name)}={N(x.Total)} ({x.Bills} bills, {N(x.People)} people, {Pct(x.Total, settledTotal)})")));

		text.AppendLine("Top 10 tables by total: " + string.Join("; ", settled
			.Where(t => !string.IsNullOrWhiteSpace(t.DiningTableName))
			.GroupBy(t => (Area: t.DiningAreaName, Table: t.DiningTableName))
			.Select(g => new { g.Key.Area, g.Key.Table, Bills = g.Count(), Total = g.Sum(t => t.TotalAmount) })
			.OrderByDescending(x => x.Total).Take(10)
			.Select(x => $"{Clean(x.Area)} {Clean(x.Table)}={N(x.Total)} ({x.Bills} bills)")));

		text.AppendLine("Online partner bills: " + string.Join("; ", settled
			.Where(t => !string.IsNullOrWhiteSpace(t.OnlinePartnerName))
			.GroupBy(t => t.OnlinePartnerName)
			.Select(g => new { Name = g.Key, Bills = g.Count(), Total = D(g.Sum(t => t.TotalAmount)) })
			.OrderByDescending(x => x.Total)
			.Select(x => $"{Clean(x.Name)}={x.Bills} bills / {N(x.Total)} ({Pct(x.Total, settledTotal)})")));

		text.AppendLine("By hour of day (settled bills): " + string.Join("; ", settled
			.GroupBy(t => t.TransactionDateTime.Hour)
			.OrderBy(g => g.Key)
			.Select(g => $"{g.Key:00}:00={g.Count()} bills / {N(g.Sum(t => t.TotalAmount))}")));

		if (days > 1)
			text.AppendLine("Top 5 days by settled total: " + string.Join("; ", settled
				.GroupBy(t => t.TransactionDateTime.Date)
				.Select(g => new { Day = g.Key, Bills = g.Count(), Total = g.Sum(t => t.TotalAmount) })
				.OrderByDescending(x => x.Total).Take(5)
				.Select(x => $"{x.Day:dd-MMM-yyyy}={N(x.Total)} ({x.Bills} bills)")));

		text.AppendLine("Top 5 customers by settled total: " + string.Join("; ", settled
			.Where(t => !string.IsNullOrWhiteSpace(t.CustomerName))
			.GroupBy(t => t.CustomerName)
			.Select(g => new { Name = g.Key, Bills = g.Count(), Total = g.Sum(t => t.TotalAmount) })
			.OrderByDescending(x => x.Total).Take(5)
			.Select(x => $"{Clean(x.Name)}={N(x.Total)} ({x.Bills} bills)")));

		text.AppendLine();
		text.AppendLine($"BILL TABLE (CSV, settled bills, one row per {grain} per location)");
		text.AppendLine("period,location,bills,people,units,total,discount,service_charge,cash,upi,card,credit");

		var rows = settled
			.GroupBy(t => (Period: t.TransactionDateTime.ToString(format), Location: t.LocationName))
			.OrderBy(g => g.Key.Period).ThenBy(g => g.Key.Location)
			.Take(600);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				g.Key.Period,
				Clean(g.Key.Location),
				g.Count().ToString(),
				N(g.Sum(t => t.TotalPeople)),
				N(g.Sum(t => t.TotalQuantity), "0.##"),
				N(g.Sum(t => t.TotalAmount)),
				N(g.Sum(t => t.DiscountAmount)),
				N(g.Sum(t => t.ServiceChargeAmount)),
				N(g.Sum(t => t.Cash)),
				N(g.Sum(t => t.UPI)),
				N(g.Sum(t => t.Card)),
				N(g.Sum(t => t.Credit))));

		return text.ToString();
	}
}

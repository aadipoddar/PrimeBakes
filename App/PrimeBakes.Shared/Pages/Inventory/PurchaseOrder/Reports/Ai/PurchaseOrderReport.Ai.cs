using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.PurchaseOrder.Reports;

public partial class PurchaseOrderReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the PURCHASE ORDER REPORT: orders placed with parties (suppliers) for raw material.
		An order is "received" when a purchase has been recorded against it and "pending" when it has not.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and lists, then an ORDER TABLE (CSV).
		CSV columns in the normal view: period, party, orders, lines (order lines), received_orders, pending_orders.
		In the SUMMARY view the table is one row per party for the whole period with lines only, so there is no received or pending split and no day split.
		Quantities are not given in this report, because raw materials use different units (kg, litre and so on) and cannot be added together. Never add quantities of different materials. For quantities, tell the person to open the Purchase Order Item Report.

		HOW THE BUSINESS WORKS
		- A pending order that is not yet due is not late. An order is "overdue" only when it is pending and its expected delivery date is before today; the data lists these.
		- Lead time is the number of days from the order date to the date the purchase was recorded. It only exists for received orders.
		- The data does not say why an order is pending or late, so never assume a cause.
		- Copy party names exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the number of purchase orders and order lines for the period, and how many are received and pending, in numbers and percentages (say the split is not available if the data says so); " +
		"(2) the party with the most orders and its pending orders; " +
		"(3) the overdue pending orders, with the party, how many days overdue, and how many there are in total; say there are none if there are none; " +
		"(4) the average lead time in days for received orders, and the party with the slowest and the fastest average lead time; " +
		"(5) one thing that needs attention from this data, such as overdue orders or deliveries due in the next seven days, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"What is still pending?",
		"Which orders are overdue?",
		"Which party delivers fastest?",
		"What is due in the next 7 days?"
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
		static string N(IFormattable value, string format = "0.##") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";

		// Dates are handled as yyyy-MM-dd text so this works whether a model date is DateTime, nullable or DateOnly
		static string Ymd(IFormattable value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
		static DateTime? Day(IFormattable value) =>
			DateTime.TryParseExact(Ymd(value), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;

		var all = _transactionOverviews;
		var days = (_toDate.Date - _fromDate.Date).Days + 1;
		var grain = days <= 92 ? "day" : "month";
		var format = days <= 92 ? "yyyy-MM-dd" : "yyyy-MM";
		var today = _aiToday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		var nextWeek = _aiToday.AddDays(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}.");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Party filter: {(_selectedParty?.Id > 0 ? _selectedParty.Name : "All")}");
		text.AppendLine($"Orders shown: {(_showPendingOnly ? "pending only" : "received and pending")}. Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");

		if (_showSummary)
		{
			text.AppendLine("This is the SUMMARY view: one row per party for the whole period, so there is no received or pending split and no day split.");
			text.AppendLine($"All parties: parties={all.Count}, lines={all.Sum(t => t.TotalItems)}");
			text.AppendLine();
			text.AppendLine("ORDER TABLE (CSV, whole period)");
			text.AppendLine("party,lines");
			foreach (var t in all.OrderByDescending(t => t.TotalItems))
				text.AppendLine(string.Join(',', Clean(t.PartyName), N(t.TotalItems)));

			return text.ToString();
		}

		var pending = all.Where(t => t.PurchaseId is null).ToList();
		var received = all.Where(t => t.PurchaseId is not null).ToList();

		text.AppendLine($"All orders: orders={all.Count}, lines={all.Sum(t => t.TotalItems)}, received={received.Count} ({Pct(received.Count, all.Count)}), pending={pending.Count} ({Pct(pending.Count, all.Count)})");

		text.AppendLine("By party, most orders first: " + string.Join("; ", all
			.GroupBy(t => t.PartyName)
			.Select(g => new { Name = g.Key, Orders = g.Count(), Lines = g.Sum(t => t.TotalItems), Pending = g.Count(t => t.PurchaseId is null) })
			.OrderByDescending(x => x.Orders)
			.Select(x => $"{Clean(x.Name)}={x.Orders} orders / {x.Lines} lines ({x.Pending} pending)")));

		var overdue = pending
			.Where(t => Ymd(t.ExpectedDeliveryDate).Length > 0 && string.CompareOrdinal(Ymd(t.ExpectedDeliveryDate), today) < 0)
			.OrderBy(t => Ymd(t.ExpectedDeliveryDate))
			.ToList();

		text.AppendLine($"Overdue pending orders (expected delivery before today): {overdue.Count}");
		text.AppendLine("Most overdue: " + string.Join("; ", overdue.Take(10)
			.Select(t => $"{Clean(t.TransactionNo)}: {Clean(t.PartyName)}, expected {Ymd(t.ExpectedDeliveryDate)}, {(_aiToday - Day(t.ExpectedDeliveryDate)!.Value).Days} days overdue")));

		text.AppendLine("Pending orders due in the next 7 days: " + string.Join("; ", pending
			.Where(t => Ymd(t.ExpectedDeliveryDate).Length > 0 && string.CompareOrdinal(Ymd(t.ExpectedDeliveryDate), today) >= 0 && string.CompareOrdinal(Ymd(t.ExpectedDeliveryDate), nextWeek) <= 0)
			.OrderBy(t => Ymd(t.ExpectedDeliveryDate)).Take(10)
			.Select(t => $"{Clean(t.TransactionNo)}: {Clean(t.PartyName)}, expected {Ymd(t.ExpectedDeliveryDate)}")));

		text.AppendLine("Oldest pending orders: " + string.Join("; ", pending
			.OrderBy(t => t.TransactionDateTime).Take(10)
			.Select(t => $"{Clean(t.TransactionNo)}: {Clean(t.PartyName)}, ordered {t.TransactionDateTime:dd-MMM-yyyy}, waiting {(_aiToday - t.TransactionDateTime.Date).Days} days")));

		var leadTimes = received
			.Select(t => new { Party = t.PartyName, Received = Day(t.PurchaseDateTime), t.TransactionDateTime })
			.Where(x => x.Received is not null)
			.Select(x => new { x.Party, Days = (x.Received!.Value - x.TransactionDateTime.Date).Days })
			.ToList();

		text.AppendLine(leadTimes.Count == 0
			? "Lead time (order to purchase recorded): no received orders in the data."
			: $"Lead time (order to purchase recorded), all received orders: average {N(leadTimes.Average(x => x.Days), "0.#")} days over {leadTimes.Count} orders");
		text.AppendLine("Average lead time by party, slowest first: " + string.Join("; ", leadTimes
			.GroupBy(x => x.Party)
			.Select(g => new { Name = g.Key, Orders = g.Count(), Average = g.Average(x => x.Days) })
			.OrderByDescending(x => x.Average)
			.Select(x => $"{Clean(x.Name)}={N(x.Average, "0.#")} days ({x.Orders} orders)")));

		text.AppendLine();
		text.AppendLine($"ORDER TABLE (CSV, one row per {grain} per party)");
		text.AppendLine("period,party,orders,lines,received_orders,pending_orders");

		var rows = all
			.GroupBy(t => (Period: t.TransactionDateTime.ToString(format), Party: t.PartyName))
			.OrderBy(g => g.Key.Period).ThenBy(g => g.Key.Party)
			.Take(600);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				g.Key.Period,
				Clean(g.Key.Party),
				g.Count().ToString(),
				N(g.Sum(t => t.TotalItems)),
				g.Count(t => t.PurchaseId is not null).ToString(),
				g.Count(t => t.PurchaseId is null).ToString()));

		return text.ToString();
	}
}

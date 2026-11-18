using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Order.Reports;

public partial class OrderReport
{
	private const int MainOutletLocationId = 1;

	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the ORDER REPORT: one row per order placed by a location. An order is "fulfilled" when a sale has been made against it and "pending" when it has not.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and lists, then an ORDER TABLE (CSV).
		CSV columns in the normal view: period, location, orders, items (order lines), units, fulfilled_orders, pending_orders.
		In the SUMMARY view the table is one row per location for the whole period with items and units only, so there is no fulfilled or pending split and no day split.

		HOW THE BUSINESS WORKS
		- A pending order that was placed recently is not late. Say "waiting N days" and nothing stronger; the data has no promised dates.
		- A location with no orders in the period is not necessarily inactive. Say "no orders in this period".
		- Never call a pending count a problem without comparing it with how long the orders have waited.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the number of orders, order lines and units for the period, and how many orders are fulfilled and pending, in numbers and percentages (say the split is not available if the data says so); " +
		"(2) the location with the most orders and the one with the fewest, among locations that ordered; if only one location is in the data, say so briefly; " +
		"(3) the oldest pending orders, with how many days they have waited; " +
		"(4) the busiest day by orders; " +
		"(5) one thing that needs attention from this data, such as orders waiting for several days or locations with no orders, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which location orders the most?",
		"What is still pending?",
		"Which orders have waited longest?",
		"Which days were busiest?"
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
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";

		var all = _transactionOverviews;
		var days = (_toDate.Date - _fromDate.Date).Days + 1;
		var grain = days <= 92 ? "day" : "month";
		var format = days <= 92 ? "yyyy-MM-dd" : "yyyy-MM";

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Location filter: {(_selectedLocation?.Id > 0 ? _selectedLocation.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Orders shown: {(_completedFilter == YesNoFilterOptions.Yes ? "fulfilled only" : _completedFilter == YesNoFilterOptions.No ? "pending only" : "fulfilled and pending")}. Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");

		if (_showSummary)
		{
			text.AppendLine("This is the SUMMARY view: one row per location for the whole period, so there is no fulfilled or pending split and no day split.");
			text.AppendLine($"All locations: locations={all.Count}, items={all.Sum(t => t.TotalItems)}, units={N(all.Sum(t => t.TotalQuantity))}");
			text.AppendLine();
			text.AppendLine("ORDER TABLE (CSV, whole period)");
			text.AppendLine("location,items,units");
			foreach (var t in all.OrderByDescending(t => t.TotalQuantity))
				text.AppendLine(string.Join(',', Clean(t.LocationName), N(t.TotalItems), N(t.TotalQuantity)));

			return text.ToString();
		}

		var pending = all.Where(t => t.SaleId is null).ToList();
		var fulfilled = all.Count - pending.Count;

		text.AppendLine($"All orders: orders={all.Count}, items={all.Sum(t => t.TotalItems)}, units={N(all.Sum(t => t.TotalQuantity))}, " +
			$"fulfilled={fulfilled} ({Pct(fulfilled, all.Count)}), pending={pending.Count} ({Pct(pending.Count, all.Count)}), pending units={N(pending.Sum(t => t.TotalQuantity))}");

		text.AppendLine("By location, most orders first: " + string.Join("; ", all
			.GroupBy(t => t.LocationName)
			.Select(g => new { Name = g.Key, Orders = g.Count(), Units = g.Sum(t => t.TotalQuantity), Pending = g.Count(t => t.SaleId is null) })
			.OrderByDescending(x => x.Orders)
			.Select(x => $"{Clean(x.Name)}={x.Orders} orders / {N(x.Units)} units ({x.Pending} pending)")));

		text.AppendLine("Oldest pending orders: " + string.Join("; ", pending
			.OrderBy(t => t.TransactionDateTime).Take(10)
			.Select(t => $"{Clean(t.TransactionNo)}: {Clean(t.LocationName)}, {N(t.TotalQuantity)} units, waiting {(_aiToday - t.TransactionDateTime.Date).Days} days")));

		if (days > 1)
			text.AppendLine("Busiest days by orders: " + string.Join("; ", all
				.GroupBy(t => t.TransactionDateTime.Date)
				.Select(g => new { Day = g.Key, Orders = g.Count(), Units = g.Sum(t => t.TotalQuantity) })
				.OrderByDescending(x => x.Orders).ThenByDescending(x => x.Units).Take(5)
				.Select(x => $"{x.Day:dd-MMM-yyyy}={x.Orders} orders / {N(x.Units)} units")));

		if (_selectedLocation is null || _selectedLocation.Id <= 0)
			text.AppendLine("Locations with no orders in the period: " + string.Join("; ", _locations
				.Where(l => l.Id != MainOutletLocationId && !all.Any(t => t.LocationName == l.Name))
				.Select(l => Clean(l.Name))));

		text.AppendLine();
		text.AppendLine($"ORDER TABLE (CSV, one row per {grain} per location)");
		text.AppendLine("period,location,orders,items,units,fulfilled_orders,pending_orders");

		var rows = all
			.GroupBy(t => (Period: t.TransactionDateTime.ToString(format), Location: t.LocationName))
			.OrderBy(g => g.Key.Period).ThenBy(g => g.Key.Location)
			.Take(600);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				g.Key.Period,
				Clean(g.Key.Location),
				g.Count().ToString(),
				N(g.Sum(t => t.TotalItems)),
				N(g.Sum(t => t.TotalQuantity)),
				g.Count(t => t.SaleId is not null).ToString(),
				g.Count(t => t.SaleId is null).ToString()));

		return text.ToString();
	}
}

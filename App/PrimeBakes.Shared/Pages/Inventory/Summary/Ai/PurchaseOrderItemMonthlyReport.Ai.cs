using PrimeBakes.Models.Inventory.PurchaseOrder;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Summary;

public partial class PurchaseOrderItemMonthlyReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the PURCHASE ORDER ITEM MONTHLY REPORT: raw material ordered from suppliers, one row per raw material, with one column per month of one financial year. An order line is "received" once a purchase has been recorded against it and "pending" until then. REPORT DATA has the financial year, filters and what is included, then PRECOMPUTED TOTALS and lists, then an ITEM TABLE (CSV).
		The month columns show the quantity ordered, in each item's own unit. This report has quantities only, no rupee amounts. Never give a money figure.
		Raw materials are measured in different units (kg, litres, pieces), so never add up or compare quantities across different items. Compare items by the number of orders they appear in. Give an item's quantity only together with its own unit.
		ITEM TABLE columns: item, category, uom, one column per month (quantity ordered), total_ordered (the 12 months added up), received (quantity received), pending (quantity still pending), orders (number of orders that include the item), suppliers (number of suppliers ordered from). Only the items with the most orders are listed; a line says how many others are left out.
		One order can contain several items, so the orders column cannot be added up across items. Use the precomputed order totals instead.
		Growth figures compare the second half of the financial year with the first half. They are indicative only while the financial year is still running.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) orders raw material from suppliers. An order is a request to a supplier, not a cost yet.
		- Recent orders are normally still waiting for the supplier, so do not call recent pending quantities late or overdue. Mention how old a pending item is only when the data shows it.
		- The current month is incomplete and months after it have not happened yet, so a small or zero value there is NOT a drop or a fall.
		- Item names can carry extra text such as a size. Keep names exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the number of orders so far this financial year and how many still have at least one pending item line; " +
		"(2) the top three raw materials by number of orders, with their orders and quantity in their own unit; " +
		"(3) the busiest and the quietest month by number of orders among months that have data, leaving out the current incomplete month; " +
		"(4) the supplier with the most orders and its share (if only one supplier is in the data, say so briefly); " +
		"(5) one thing that needs attention, such as items with a lot of pending quantity or an item not ordered for months, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which items are still pending?",
		"Most ordered raw materials",
		"Which months had the most orders?",
		"Which supplier gets the most orders?"
	];

	private bool _showAi = false;

	private async Task ToggleAi()
	{
		_showAi = !_showAi;
		if (_showAi)
			_currentDateTime = await CommonData.LoadCurrentDateTime();
	}

	// Text version of what is on screen: _monthlySummaries is the grid, _transactionOverviews is the filtered data behind it.
	// The totals are computed here, because the model is unreliable at adding up hundreds of rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static string Pct(double part, double whole) => whole == 0 ? "n/a" : $"{(part * 100 / whole).ToString("0.#", CultureInfo.InvariantCulture)}%";
		static string OrNone(IEnumerable<string> parts) { var list = parts.ToList(); return list.Count == 0 ? "none" : string.Join("; ", list); }
		static bool IsPending(PurchaseOrderItemOverviewModel t) => t.PurchaseId is null;

		int MonthIndex(DateTime date) => ((date.Year - _fromDate.Year) * 12) + date.Month - _fromDate.Month;
		string PartyName(int? id) => Clean(_parties.FirstOrDefault(p => p.Id == id)?.Name ?? "Unknown");

		var rows = _transactionOverviews;
		var summaries = _monthlySummaries;
		var months = _monthHeaders.Select(Clean).ToList();
		var currentIndex = MonthIndex(_currentDateTime);
		var yearRunning = currentIndex is >= 0 and <= 11;
		var orders = rows.Select(t => t.MasterId).Distinct().Count();

		var text = new StringBuilder();
		text.AppendLine($"Financial year shown: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		text.AppendLine($"Month columns: {string.Join(", ", months)}");
		text.AppendLine($"Today is {_currentDateTime:dd-MMM-yyyy}. " + (yearRunning
			? $"The current month ({months[currentIndex]}) is incomplete and the months after it have no data yet."
			: currentIndex > 11 ? "This financial year is over." : "This financial year has not started."));
		text.AppendLine($"Raw material filter: {(_selectedRawMaterial?.Id > 0 ? _selectedRawMaterial.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedRawMaterialCategory?.Id > 0 ? _selectedRawMaterialCategory.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Party filter: {(_selectedParty?.Id > 0 ? _selectedParty.Name : "All")}");
		text.AppendLine($"Pending lines only: {(_showPendingOnly ? "yes, only item lines still pending are in this data" : "no")}. Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Orders: orders={orders}, orders with at least one pending item line={rows.Where(IsPending).Select(t => t.MasterId).Distinct().Count()}, " +
			$"item lines={rows.Count}, pending item lines={rows.Count(IsPending)}");
		text.AppendLine($"Raw materials in this data: {summaries.Count}; with pending quantity: {summaries.Count(s => s.PendingQuantity > 0)}");

		text.AppendLine("Orders by month: " + string.Join("; ", Enumerable.Range(0, 12)
			.Select(i => (Month: months[i], Rows: rows.Where(t => MonthIndex(t.TransactionDateTime) == i).ToList()))
			.Select(x => $"{x.Month}={x.Rows.Select(t => t.MasterId).Distinct().Count()} orders ({x.Rows.Where(IsPending).Select(t => t.MasterId).Distinct().Count()} with pending lines)")));

		text.AppendLine("Top 10 suppliers by number of orders: " + string.Join("; ", rows
			.GroupBy(t => t.PartyId)
			.Select(g => (Id: g.Key, Orders: g.Select(t => t.MasterId).Distinct().Count()))
			.OrderByDescending(x => x.Orders).Take(10)
			.Select(x => $"{PartyName(x.Id)}={x.Orders} orders (share {Pct(x.Orders, orders)})")));

		var ranked = summaries.OrderByDescending(s => s.OrderCount).ThenBy(s => s.ItemName).ToList();

		text.AppendLine("Top 15 raw materials by number of orders: " + string.Join("; ", ranked.Take(15)
			.Select(s => $"{Clean(s.ItemName)}={s.OrderCount} orders / {N(s.Total, "0.##")} {Clean(s.UnitOfMeasurement)} ordered / {s.PartyCount} suppliers")));

		text.AppendLine("Raw materials with the highest share of pending quantity (pending quantity / share of the ordered quantity): " + OrNone(summaries
			.Where(s => s.PendingQuantity > 0).OrderByDescending(s => s.PendingPercent).ThenByDescending(s => s.OrderCount).Take(10)
			.Select(s => $"{Clean(s.ItemName)}={N(s.PendingQuantity, "0.##")} {Clean(s.UnitOfMeasurement)} / {N(s.PendingPercent, "0.#")}%")));

		text.AppendLine($"Growth in quantity ordered, second half vs first half of the year{(yearRunning ? " (the year is still running, so the second half is incomplete; indicative only)" : string.Empty)}, for items active in at least 3 months:");
		var growth = summaries.Where(s => s.FirstHalf > 0 && s.ActiveMonths >= 3).ToList();
		text.AppendLine("- Growing: " + OrNone(growth.Where(s => s.GrowthPercent > 0).OrderByDescending(s => s.GrowthPercent).Take(5)
			.Select(s => $"{Clean(s.ItemName)}=+{N(s.GrowthPercent, "0.#")}%")));
		text.AppendLine("- Shrinking: " + OrNone(growth.Where(s => s.GrowthPercent < 0).OrderBy(s => s.GrowthPercent).Take(5)
			.Select(s => $"{Clean(s.ItemName)}={N(s.GrowthPercent, "0.#")}%")));

		if (yearRunning)
			text.AppendLine("Items not ordered for 2 or more months (months since the last order): " + OrNone(summaries
				.Where(s => s.MonthsSinceLastOrder >= 2).OrderByDescending(s => s.MonthsSinceLastOrder).Take(10)
				.Select(s => $"{Clean(s.ItemName)}={s.MonthsSinceLastOrder}")));

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV)");
		text.AppendLine("item,category,uom," + string.Join(',', months) + ",total_ordered,received,pending,orders,suppliers");

		foreach (var s in ranked.Take(60))
			text.AppendLine(string.Join(',',
				Clean(s.ItemName),
				Clean(s.ItemCategoryName),
				Clean(s.UnitOfMeasurement),
				string.Join(',', Enumerable.Range(0, 12).Select(i => N(s[i], "0.##"))),
				N(s.Total, "0.##"),
				N(s.FulfilledQuantity, "0.##"),
				N(s.PendingQuantity, "0.##"),
				s.OrderCount.ToString(),
				s.PartyCount.ToString()));

		if (ranked.Count > 60)
			text.AppendLine($"(Other {ranked.Count - 60} raw materials with fewer orders are left out of the table.)");

		return text.ToString();
	}
}

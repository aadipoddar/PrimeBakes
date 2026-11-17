using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Summary;

public partial class OrderItemMonthlyReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the ORDER ITEM MONTHLY REPORT: units ordered per item, month by month, for one financial year.
		An order line is "fulfilled" when a sale has been made against it and "pending" when it has not.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and lists, then an ITEM TABLE (CSV) with the top 100 items by units ordered.
		CSV columns: rank, item, category, total (units ordered), fulfilled, pending, orders (orders containing the item), locations (locations that ordered it), then one column per month of the financial year holding units ordered.
		The current month is incomplete and later months are in the future, so a low or zero value there is not a drop. The data says which month is current.

		HOW THE BUSINESS WORKS
		- Product names can carry a price tag such as "@20". Keep it exactly as written.
		- Rank items by units unless the question asks for something else. Quote the precomputed lists instead of adding up months yourself.
		- An item that is missing from the data was not ordered in the period; say "no orders" rather than calling it a bad seller.
		- Pending quantity is only "not yet sold against the order". Do not guess why.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total units ordered in the period, of which fulfilled and pending, in units and as percentages; " +
		"(2) the top three items by units, with their units and shares; " +
		"(3) the busiest complete month, and the last complete month against the month before it as a percentage change; " +
		"(4) the items that gained and lost the most between those two months, one of each; " +
		"(5) one thing that needs attention from this data, such as a large pending quantity or a top item that stopped being ordered, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top 10 ordered items",
		"What is still pending?",
		"Which items are growing?",
		"Which items stopped being ordered?"
	];

	// ---------- AI wiring ----------
	private bool _showAi = false;

	private void ToggleAi() => _showAi = !_showAi;

	// Text version of what is on screen (_monthlySummaries already has every filter applied).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0.##") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";

		string Month(int index) => Clean(_monthHeaders[index]);

		var all = _monthlySummaries;
		var financialYearStart = DateOnly.FromDateTime(_fromDate);
		var current = ((_currentDateTime.Year - financialYearStart.Year) * 12) + _currentDateTime.Month - financialYearStart.Month;
		var lastShown = Math.Min(current, 11);                  // later months are in the future
		var lastComplete = current > 11 ? 11 : current - 1;    // index of the last complete month

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy} (one financial year)");
		text.AppendLine($"Today is {_currentDateTime:dd-MMM-yyyy}." + (current is >= 0 and <= 11 ? $" {Month(current)} is the current month, so it is incomplete, and later months are in the future." : string.Empty));
		text.AppendLine($"Item filter: {(_selectedProduct?.Id > 0 ? _selectedProduct.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedProductCategory?.Id > 0 ? _selectedProductCategory.Name : "All")}");
		text.AppendLine($"Location filter: {(_selectedLocation?.Id > 0 ? _selectedLocation.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Orders shown: {(_completedFilter == YesNoFilterOptions.Yes ? "fulfilled only" : _completedFilter == YesNoFilterOptions.No ? "pending only" : "fulfilled and pending")}. Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		var total = D(all.Sum(s => s.Total));
		var fulfilled = D(all.Sum(s => s.FulfilledQuantity));
		var pending = D(all.Sum(s => s.PendingQuantity));

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All items: items={all.Count}, units ordered={N(total)}, fulfilled={N(fulfilled)} ({Pct(fulfilled, fulfilled + pending)}), pending={N(pending)} ({Pct(pending, fulfilled + pending)})");

		if (lastShown >= 0)
			text.AppendLine("Units ordered per month: " + string.Join("; ", Enumerable.Range(0, lastShown + 1)
				.Select(i => $"{Month(i)}={N(all.Sum(s => s[i]))}{(i == current ? " (incomplete)" : string.Empty)}")));

		if (lastComplete >= 1)
		{
			var now = D(all.Sum(s => s[lastComplete]));
			var before = D(all.Sum(s => s[lastComplete - 1]));
			text.AppendLine($"Last complete month {Month(lastComplete)} against {Month(lastComplete - 1)}: {N(now)} against {N(before)} units ({(before == 0 ? "no base to compare" : Pct(now - before, before) + " change")})");

			var movers = all
				.Select(s => new { s.ItemName, Now = D(s[lastComplete]), Before = D(s[lastComplete - 1]) })
				.Where(x => x.Now != 0 || x.Before != 0)
				.Select(x => new { x.ItemName, x.Now, x.Before, Change = x.Now - x.Before })
				.ToList();

			text.AppendLine("Biggest gainers in the last complete month: " + string.Join("; ", movers
				.Where(x => x.Change > 0).OrderByDescending(x => x.Change).Take(5)
				.Select(x => $"{Clean(x.ItemName)}: {N(x.Before)} to {N(x.Now)} units")));
			text.AppendLine("Biggest decliners in the last complete month: " + string.Join("; ", movers
				.Where(x => x.Change < 0).OrderBy(x => x.Change).Take(5)
				.Select(x => $"{Clean(x.ItemName)}: {N(x.Before)} to {N(x.Now)} units")));
		}

		text.AppendLine("Top 15 items by units ordered: " + string.Join("; ", all.Take(15)
			.Select(s => $"{Clean(s.ItemName)}: {N(s.Total)} units, share {N(s.ContributionPercent)}%, fulfilled {N(s.FulfilledQuantity)}, pending {N(s.PendingQuantity)}, peak month {Clean(s.PeakMonthName)}, ordered in {s.ActiveMonths} months")));

		text.AppendLine("Largest pending quantities: " + string.Join("; ", all
			.Where(s => s.PendingQuantity > 0).OrderByDescending(s => s.PendingQuantity).Take(10)
			.Select(s => $"{Clean(s.ItemName)}: {N(s.PendingQuantity)} pending of {N(s.Total)} ordered")));

		text.AppendLine("Categories by units ordered: " + string.Join("; ", all
			.GroupBy(s => s.ItemCategoryName)
			.Select(g => new { Name = g.Key, Units = D(g.Sum(s => s.Total)) })
			.OrderByDescending(x => x.Units)
			.Select(x => $"{Clean(x.Name)}={N(x.Units)} ({Pct(x.Units, total)})")));

		text.AppendLine("Items not ordered for 2 months or more (by units ordered earlier): " + string.Join("; ", all
			.Where(s => s.MonthsSinceLastOrder >= 2).OrderByDescending(s => s.Total).Take(10)
			.Select(s => $"{Clean(s.ItemName)}: {N(s.Total)} units, last order {s.MonthsSinceLastOrder} months ago")));

		if (_selectedLocation is null || _selectedLocation.Id <= 0)
			text.AppendLine("Locations with no orders in the period: " + string.Join("; ", _pendingLocations.Take(20).Select(l => Clean(l.Name))));

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV, top 100 items by units ordered)");
		text.AppendLine("rank,item,category,total,fulfilled,pending,orders,locations," + string.Join(',', _monthHeaders.Select(h => Clean(h))));

		foreach (var s in all.Take(100))
		{
			var cells = new List<string>
			{
				s.Rank.ToString(),
				Clean(s.ItemName),
				Clean(s.ItemCategoryName),
				N(s.Total),
				N(s.FulfilledQuantity),
				N(s.PendingQuantity),
				s.OrderCount.ToString(),
				s.LocationCount.ToString()
			};
			cells.AddRange(Enumerable.Range(0, 12).Select(i => N(s[i])));
			text.AppendLine(string.Join(',', cells));
		}

		return text.ToString();
	}
}

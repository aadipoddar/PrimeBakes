using PrimeBakes.Models.Store.Sale;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Summary;

public partial class SaleItemMonthlyReport
{
	private const int MainOutletLocationId = 1;

	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the SALE ITEM MONTHLY REPORT: sales per item, month by month, for one financial year.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and lists, then an ITEM TABLE (CSV) with the top 100 outlet items by units.
		CSV columns: item, category, units, net (net value), then one column per month of the financial year. The month columns hold units or net value, as the data states.
		Negative units and values are sale returns. Returns, stock transfers and restaurant bills are present only when the data says they are included; if it says returns are not included, say so and give no returns figure.
		The current month is incomplete and later months are in the future, so a low or zero value there is not a drop. The data says which month is current.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It supplies products to the other outlets on credit. That is internal supply, not customer sales. Its discounts are part of the supply terms to outlets, so do not present them as a concern.
		- For any question about best sellers, trends, categories or what customers buy, use the outlet figures only and say that supply is left out. The tables and lists below are outlet figures; supply is given separately in the totals.
		- Rank items by units unless the question asks for value. Quote the precomputed lists instead of adding up months yourself.
		- Product names can carry a price tag such as "@20". Keep it exactly as written.
		- The data only contains items with recorded sales. An item that is missing has no recorded sales; do not call it a bad seller.
		- Outlet sales reach the software in three ways: some outlets bill live, some have the whole day's sales entered at the end of the day, and for some only the remaining amount is handed over to the main outlet. So low or missing sales at an outlet are NOT necessarily weak performance. Say "recorded sales".
		- Returns are either damaged stock sent back to the main outlet or customers returning items. The data does not say which, so call them "returns" and never assume a cause.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) outlet units and net value for the period, then the main outlet's supply units, then returns (say they are not included if the data says so); " +
		"(2) the top three items by units, with their units; " +
		"(3) the busiest complete month for outlet sales, and the last complete month against the month before it as a percentage change; " +
		"(4) the items that gained and lost the most between those two months, one of each; " +
		"(5) one thing that needs attention from this data, such as a good seller with no recorded sales in the last two months, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top 10 selling items",
		"Which items are growing?",
		"Which items stopped selling?",
		"Which category earns the most?"
	];

	// ---------- AI wiring ----------
	private bool _showAi = false;

	private void ToggleAi() => _showAi = !_showAi;

	// Text version of what is on screen (_transactionOverviews already has every filter applied).
	// Built from the rows, not from _monthlySummaries, so the main outlet's supply can be kept apart from outlet sales.
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0.##") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";
		static bool IsSupply(SaleItemOverviewModel t) => t.LocationId == MainOutletLocationId;

		string Month(int index) => Clean(_monthHeaders[index]);

		var financialYearStart = DateOnly.FromDateTime(_fromDate);
		int MonthIndex(DateTime date) => ((date.Year - financialYearStart.Year) * 12) + date.Month - financialYearStart.Month;

		var current = MonthIndex(_currentDateTime);
		var lastShown = Math.Min(current, 11);                  // later months are in the future
		var lastComplete = current > 11 ? 11 : current - 1;    // index of the last complete month
		var measure = _showAmount ? "net value" : "units";

		var all = _transactionOverviews;
		var outlets = all.Where(t => !IsSupply(t)).ToList();
		var supply = all.Where(IsSupply).ToList();

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy} (one financial year)");
		text.AppendLine($"Today is {_currentDateTime:dd-MMM-yyyy}." + (current is >= 0 and <= 11 ? $" {Month(current)} is the current month, so it is incomplete, and later months are in the future." : string.Empty));
		text.AppendLine($"Month columns hold: {measure}");
		text.AppendLine($"Item filter: {(_selectedProduct?.Id > 0 ? _selectedProduct.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedProductCategory?.Id > 0 ? _selectedProductCategory.Name : "All")}");
		text.AppendLine($"Location filter: {(_selectedLocation?.Id > 0 ? _selectedLocation.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Party filter: {(_selectedParty?.Id > 0 ? _selectedParty.Name : "All")}");
		text.AppendLine($"Stock transfers: {(_showStockTransfers ? "included" : "not included")}. Restaurant bills: {(_showBills ? "included" : "not included")}. Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		var returnRows = all.Where(t => t.Quantity < 0).ToList();
		text.AppendLine(!_showSaleReturns ? "Sale returns: NOT included in this data, so there is no returns figure."
			: $"Sale returns: included as negative rows; {N(-returnRows.Sum(t => t.Quantity))} units, net value {(returnRows.Sum(t => t.NetTotal) == 0 ? "not recorded (0)" : N(-returnRows.Sum(t => t.NetTotal)))}.");

		// One entry per outlet item, with its months in the chosen measure
		var items = outlets
			.GroupBy(t => t.ItemId)
			.Select(g =>
			{
				var months = new decimal[12];
				foreach (var t in g)
				{
					var index = MonthIndex(t.TransactionDateTime);
					if (index is >= 0 and <= 11)
						months[index] += _showAmount ? D(t.NetTotal) : D(t.Quantity);
				}

				return new
				{
					Name = g.First().ItemName,
					Category = g.First().ItemCategoryName,
					Units = D(g.Sum(t => t.Quantity)),
					Net = D(g.Sum(t => t.NetTotal)),
					Last = g.Max(t => t.TransactionDateTime),
					Months = months
				};
			})
			.OrderByDescending(x => x.Units).ThenBy(x => x.Name)
			.ToList();

		var outletUnits = D(outlets.Sum(t => t.Quantity));
		var outletNet = D(outlets.Sum(t => t.NetTotal));

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Outlet sales (all outlets except the main outlet): items={items.Count}, units={N(outletUnits)}, net={N(outletNet)}");
		text.AppendLine($"Main outlet internal supply (PRIME BAKES OFFICE): units={N(supply.Sum(t => t.Quantity))}, net={N(supply.Sum(t => t.NetTotal))}");

		if (lastShown >= 0)
			text.AppendLine($"Outlet {measure} per month: " + string.Join("; ", Enumerable.Range(0, lastShown + 1)
				.Select(i => $"{Month(i)}={N(items.Sum(x => x.Months[i]))}{(i == current ? " (incomplete)" : string.Empty)}")));

		if (lastComplete >= 1)
		{
			var now = items.Sum(x => x.Months[lastComplete]);
			var before = items.Sum(x => x.Months[lastComplete - 1]);
			text.AppendLine($"Last complete month {Month(lastComplete)} against {Month(lastComplete - 1)} (outlet {measure}): {N(now)} against {N(before)} ({(before == 0 ? "no base to compare" : Pct(now - before, before) + " change")})");

			var movers = items
				.Select(x => new { x.Name, Now = x.Months[lastComplete], Before = x.Months[lastComplete - 1] })
				.Where(x => x.Now != 0 || x.Before != 0)
				.Select(x => new { x.Name, x.Now, x.Before, Change = x.Now - x.Before })
				.ToList();

			text.AppendLine($"Biggest gainers in the last complete month ({measure}): " + string.Join("; ", movers
				.Where(x => x.Change > 0).OrderByDescending(x => x.Change).Take(5)
				.Select(x => $"{Clean(x.Name)}: {N(x.Before)} to {N(x.Now)}")));
			text.AppendLine($"Biggest decliners in the last complete month ({measure}): " + string.Join("; ", movers
				.Where(x => x.Change < 0).OrderBy(x => x.Change).Take(5)
				.Select(x => $"{Clean(x.Name)}: {N(x.Before)} to {N(x.Now)}")));
		}

		text.AppendLine("Top 15 outlet items by units: " + string.Join("; ", items.Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Units)} units / {N(x.Net, "0")}")));
		text.AppendLine("Top 10 outlet items by net value: " + string.Join("; ", items.OrderByDescending(x => x.Net).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Net, "0")} / {N(x.Units)} units")));
		text.AppendLine("Outlet categories by net value: " + string.Join("; ", items
			.GroupBy(x => x.Category)
			.Select(g => new { Name = g.Key, Units = g.Sum(x => x.Units), Net = g.Sum(x => x.Net) })
			.OrderByDescending(x => x.Net)
			.Select(x => $"{Clean(x.Name)}={N(x.Net, "0")} ({Pct(x.Net, outletNet)}) / {N(x.Units)} units")));

		text.AppendLine("Items with no recorded outlet sales for 2 months or more (by units sold earlier): " + string.Join("; ", items
			.Where(x => MonthsBetween(x.Last, _currentDateTime) >= 2).Take(10)
			.Select(x => $"{Clean(x.Name)}: {N(x.Units)} units, last sale {MonthsBetween(x.Last, _currentDateTime)} months ago")));

		text.AppendLine();
		text.AppendLine($"ITEM TABLE (CSV, top 100 outlet items by units; month columns hold {measure})");
		text.AppendLine("item,category,units,net," + string.Join(',', _monthHeaders.Select(h => Clean(h))));

		foreach (var x in items.Take(100))
		{
			var cells = new List<string> { Clean(x.Name), Clean(x.Category), N(x.Units), N(x.Net, "0") };
			cells.AddRange(x.Months.Select(m => N(m)));
			text.AppendLine(string.Join(',', cells));
		}

		return text.ToString();
	}

	private static int MonthsBetween(DateTime from, DateTime to) => ((to.Year - from.Year) * 12) + to.Month - from.Month;
}

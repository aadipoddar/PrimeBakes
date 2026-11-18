using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Order.Reports;

public partial class OrderItemReport
{
	private const int MainOutletLocationId = 1;

	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the ORDER ITEM REPORT: the items on orders placed by locations. An order line is "fulfilled" when a sale has been made against its order and "pending" when it has not.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and rankings, then an ITEM TABLE (CSV) with the top 100 items by units ordered.
		CSV columns in the normal view: item, category, units, fulfilled (units), pending (units), orders (orders containing the item), locations (locations that ordered it).
		In the SUMMARY view the table is one row per item for the whole period with units only, so there is no fulfilled or pending split and no location split.
		The data only contains items that were ordered in the period. An item that is missing was not ordered; say "no orders" rather than calling it a bad seller.

		HOW THE BUSINESS WORKS
		- Product names can carry a price tag such as "@20". Keep it exactly as written.
		- Rank items by units unless the question asks for something else. Quote the precomputed lists instead of adding up rows yourself.
		- Pending only means "not yet sold against the order". A pending line that was ordered recently is not late, and the data does not say why a line is pending.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total units ordered, how many different items and orders, and the fulfilled and pending units as percentages (say the split is not available if the data says so); " +
		"(2) the top three items by units ordered, with their units; " +
		"(3) the category with the most units ordered and its share as a percentage; " +
		"(4) the items with the largest pending units; " +
		"(5) one thing that needs attention from this data, such as a large pending quantity or a location with no orders, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top 10 ordered items",
		"What is still pending?",
		"Which category is ordered most?",
		"What does each location order most?"
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

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Item filter: {(_selectedProduct?.Id > 0 ? _selectedProduct.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedProductCategory?.Id > 0 ? _selectedProductCategory.Name : "All")}");
		text.AppendLine($"Location filter: {(_selectedLocation?.Id > 0 ? _selectedLocation.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Orders shown: {(_completedFilter == YesNoFilterOptions.Yes ? "fulfilled only" : _completedFilter == YesNoFilterOptions.No ? "pending only" : "fulfilled and pending")}. Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");

		if (_showSummary)
		{
			var summaryTotal = D(all.Sum(t => t.Quantity));
			text.AppendLine("This is the SUMMARY view: one row per item for the whole period, so there is no fulfilled or pending split and no location split.");
			text.AppendLine($"All items: items={all.Count}, units={N(summaryTotal)}");
			text.AppendLine("Top 15 items by units: " + string.Join("; ", all.OrderByDescending(t => t.Quantity).Take(15)
				.Select(t => $"{Clean(t.ItemName)}={N(t.Quantity)}")));
			text.AppendLine("Categories by units: " + string.Join("; ", all
				.GroupBy(t => t.ItemCategoryName)
				.Select(g => new { Name = g.Key, Units = D(g.Sum(t => t.Quantity)) })
				.OrderByDescending(x => x.Units)
				.Select(x => $"{Clean(x.Name)}={N(x.Units)} ({Pct(x.Units, summaryTotal)})")));
			text.AppendLine();
			text.AppendLine("ITEM TABLE (CSV, whole period)");
			text.AppendLine("item,category,units");
			foreach (var t in all.OrderByDescending(t => t.Quantity).Take(100))
				text.AppendLine(string.Join(',', Clean(t.ItemName), Clean(t.ItemCategoryName), N(t.Quantity)));

			return text.ToString();
		}

		var totalUnits = D(all.Sum(t => t.Quantity));
		var pendingUnits = D(all.Where(t => t.SaleId is null).Sum(t => t.Quantity));
		var fulfilledUnits = totalUnits - pendingUnits;

		text.AppendLine($"All order lines: lines={all.Count}, different items={all.Select(t => t.ItemName).Distinct().Count()}, orders={all.Select(t => t.MasterId).Distinct().Count()}, " +
			$"units={N(totalUnits)}, fulfilled={N(fulfilledUnits)} ({Pct(fulfilledUnits, totalUnits)}), pending={N(pendingUnits)} ({Pct(pendingUnits, totalUnits)})");

		var items = all
			.GroupBy(t => t.ItemName)
			.Select(g => new
			{
				Name = g.Key,
				Category = g.First().ItemCategoryName,
				Units = g.Sum(t => t.Quantity),
				Fulfilled = g.Where(t => t.SaleId is not null).Sum(t => t.Quantity),
				Pending = g.Where(t => t.SaleId is null).Sum(t => t.Quantity),
				Orders = g.Select(t => t.MasterId).Distinct().Count(),
				Locations = g.Select(t => t.LocationName).Distinct().Count()
			})
			.OrderByDescending(x => x.Units).ThenBy(x => x.Name)
			.ToList();

		text.AppendLine("Top 15 items by units ordered: " + string.Join("; ", items.Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Units)} units ({N(x.Pending)} pending, {x.Orders} orders, {x.Locations} locations)")));
		text.AppendLine("Largest pending units: " + string.Join("; ", items
			.Where(x => x.Pending > 0).OrderByDescending(x => x.Pending).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Pending)} pending of {N(x.Units)}")));
		text.AppendLine("Categories by units ordered: " + string.Join("; ", items
			.GroupBy(x => x.Category)
			.Select(g => new { Name = g.Key, Units = D(g.Sum(x => x.Units)) })
			.OrderByDescending(x => x.Units)
			.Select(x => $"{Clean(x.Name)}={N(x.Units)} ({Pct(x.Units, totalUnits)})")));

		text.AppendLine("By location, most units first: " + string.Join("; ", all
			.GroupBy(t => t.LocationName)
			.Select(g => new { Name = g.Key, Units = g.Sum(t => t.Quantity), Orders = g.Select(t => t.MasterId).Distinct().Count() })
			.OrderByDescending(x => x.Units)
			.Select(x => $"{Clean(x.Name)}={N(x.Units)} units / {x.Orders} orders")));

		text.AppendLine("Top 3 items by units at each location:");
		foreach (var location in all.GroupBy(t => t.LocationName).OrderBy(g => g.Key))
			text.AppendLine($"- {Clean(location.Key)}: " + string.Join("; ", location
				.GroupBy(t => t.ItemName)
				.Select(g => new { Name = g.Key, Units = g.Sum(t => t.Quantity) })
				.OrderByDescending(x => x.Units).Take(3)
				.Select(x => $"{Clean(x.Name)}={N(x.Units)}")));

		if (_selectedLocation is null || _selectedLocation.Id <= 0)
			text.AppendLine("Locations with no orders in the period: " + string.Join("; ", _locations
				.Where(l => l.Id != MainOutletLocationId && !all.Any(t => t.LocationName == l.Name))
				.Select(l => Clean(l.Name))));

		if (days > 1)
		{
			var (grain, format) = days <= 92 ? ("Daily", "yyyy-MM-dd") : ("Monthly", "yyyy-MM");
			text.AppendLine($"{grain} units ordered: " + string.Join("; ", all
				.GroupBy(t => t.TransactionDateTime.ToString(format))
				.OrderBy(g => g.Key)
				.Select(g => $"{g.Key}={N(g.Sum(t => t.Quantity))}")));
		}

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV, top 100 items by units ordered)");
		text.AppendLine("item,category,units,fulfilled,pending,orders,locations");

		foreach (var x in items.Take(100))
			text.AppendLine(string.Join(',',
				Clean(x.Name), Clean(x.Category), N(x.Units), N(x.Fulfilled), N(x.Pending), x.Orders.ToString(), x.Locations.ToString()));

		return text.ToString();
	}
}

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Sale.Reports;

public partial class SaleReturnItemReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the SALE RETURN ITEM REPORT: returned items, broken down by item (product). It lists returns only; there are no sales in this data. So never work out a return rate and never compare returns with sales.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and rankings, then an ITEM TABLE (CSV).
		CSV columns in the normal view: item, category, location, units (quantity returned), net (net value), total (line total after tax).
		In the SUMMARY view the table is one row per item for the whole period, with no location or day split.
		All units and values are positive: they are what was returned. The data only contains items that have returns in the period.
		If net is zero while units are not, say that the net value is not recorded for those returns instead of calling them worthless.

		HOW THE BUSINESS WORKS
		- Returns are either damaged stock sent back from outlets to the main outlet, or customers returning items to outlets. The data does not say which, so call them "returns" and never assume a cause. You may point out that an item or outlet stands out, but never say why.
		- Without sales to compare against, do not call a returned quantity high or low. Compare only items, categories, locations and days within this data.
		- Product names can carry a price tag such as "@20". Keep it exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total units and net value returned in the period, and how many different items were returned; " +
		"(2) the top three items by units returned, with their units; " +
		"(3) the category with the most units returned and its share of all returned units as a percentage; " +
		"(4) the location with the most units returned and its top returned item; if only one location is in the data, say so briefly; " +
		"(5) one thing that needs attention from this data, such as one item returned at several locations or on many days, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top 10 returned items",
		"Which category has the most returns?",
		"Which location returns the most?",
		"Items returned at several outlets"
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
	// Rankings and totals are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');

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
		text.AppendLine($"Party filter: {(_selectedParty?.Id > 0 ? _selectedParty.Name : "All")}");
		text.AppendLine($"Online partner filter: {(_selectedOnlinePartner?.Id > 0 ? _selectedOnlinePartner.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");
		if (_showSummary)
			text.AppendLine("This is the SUMMARY view: one row per item for the whole period, so there is no split by location or day.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All returns: units={N(all.Sum(t => t.Quantity), "0.##")}, net={N(all.Sum(t => t.NetTotal))}, total={N(all.Sum(t => t.Total))}, " +
			$"discount={N(all.Sum(t => t.DiscountAmount))}, different items={all.Select(t => t.ItemName).Distinct().Count()}");

		var items = all
			.GroupBy(t => t.ItemName)
			.Select(g => new { Name = g.Key, Qty = g.Sum(t => t.Quantity), Net = g.Sum(t => t.NetTotal) })
			.ToList();

		text.AppendLine("Top 15 items by units returned: " + string.Join("; ", items.OrderByDescending(x => x.Qty).Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Qty, "0.##")} units / {N(x.Net)}")));
		text.AppendLine("Top 10 items by net value returned: " + string.Join("; ", items.OrderByDescending(x => x.Net).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Qty, "0.##")} units")));
		text.AppendLine("Categories by units returned: " + string.Join("; ", all
			.GroupBy(t => t.ItemCategoryName)
			.Select(g => new { Name = g.Key, Qty = g.Sum(t => t.Quantity), Net = g.Sum(t => t.NetTotal) })
			.OrderByDescending(x => x.Qty)
			.Select(x => $"{Clean(x.Name)}={N(x.Qty, "0.##")} units / {N(x.Net)}")));

		if (!_showSummary)
		{
			text.AppendLine("By location, most units first: " + string.Join("; ", all
				.GroupBy(t => t.LocationName)
				.Select(g => new { Name = g.Key, Qty = g.Sum(t => t.Quantity), Net = g.Sum(t => t.NetTotal) })
				.OrderByDescending(x => x.Qty)
				.Select(x => $"{Clean(x.Name)}={N(x.Qty, "0.##")} units / {N(x.Net)}")));

			text.AppendLine("Top 3 returned items at each location:");
			foreach (var location in all.GroupBy(t => t.LocationName).OrderBy(g => g.Key))
				text.AppendLine($"- {Clean(location.Key)}: " + string.Join("; ", location
					.GroupBy(t => t.ItemName)
					.Select(g => new { Name = g.Key, Qty = g.Sum(t => t.Quantity) })
					.OrderByDescending(x => x.Qty).Take(3)
					.Select(x => $"{Clean(x.Name)}={N(x.Qty, "0.##")}")));

			text.AppendLine("Items returned at more than one location: " + string.Join("; ", all
				.GroupBy(t => t.ItemName)
				.Select(g => new { Name = g.Key, Locations = g.Select(t => t.LocationName).Distinct().Count(), Qty = g.Sum(t => t.Quantity) })
				.Where(x => x.Locations > 1)
				.OrderByDescending(x => x.Locations).ThenByDescending(x => x.Qty).Take(10)
				.Select(x => $"{Clean(x.Name)}={x.Locations} locations / {N(x.Qty, "0.##")} units")));

			if (days > 1)
			{
				var (grain, format) = days <= 92 ? ("Daily", "dd-MMM-yyyy") : ("Monthly", "MMM-yyyy");
				text.AppendLine($"{grain} returns (units / net value): " + string.Join("; ", all
					.GroupBy(t => t.TransactionDateTime.ToString(days <= 92 ? "yyyy-MM-dd" : "yyyy-MM"))
					.OrderBy(g => g.Key)
					.Select(g => $"{g.First().TransactionDateTime.ToString(format)}={N(g.Sum(t => t.Quantity), "0.##")} / {N(g.Sum(t => t.NetTotal))}")));
			}
		}

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV, whole period)");

		if (_showSummary)
		{
			text.AppendLine("item,category,units,net,total");
			foreach (var t in all.OrderBy(t => t.ItemCategoryName).ThenBy(t => t.ItemName))
				text.AppendLine(string.Join(',', Clean(t.ItemName), Clean(t.ItemCategoryName), N(t.Quantity, "0.##"), N(t.NetTotal), N(t.Total)));

			return text.ToString();
		}

		text.AppendLine("item,category,location,units,net,total");
		var rows = all
			.GroupBy(t => (Item: t.ItemName, Category: t.ItemCategoryName, Location: t.LocationName))
			.OrderBy(g => g.Key.Category).ThenBy(g => g.Key.Item).ThenBy(g => g.Key.Location);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				Clean(g.Key.Item),
				Clean(g.Key.Category),
				Clean(g.Key.Location),
				N(g.Sum(t => t.Quantity), "0.##"),
				N(g.Sum(t => t.NetTotal)),
				N(g.Sum(t => t.Total))));

		return text.ToString();
	}
}

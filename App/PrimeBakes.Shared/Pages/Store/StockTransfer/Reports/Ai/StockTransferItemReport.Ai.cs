using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.StockTransfer.Reports;

public partial class StockTransferItemReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the STOCK TRANSFER ITEM REPORT: the items moved from one location (from) to another (to), valued like a sale.
		These are internal stock movements, not customer sales.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and rankings, then an ITEM TABLE (CSV) with the top 100 items by units transferred.
		CSV columns in the normal view: item, category, units, net (net value), discount, locations_received (number of receiving locations).
		In the SUMMARY view the table is one row per item for the whole period, with no location split and no day split.
		The data only contains items that were transferred in the period. An item that is missing was not transferred; say "no transfers".

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). Transfers from it are supply to the other locations. What a location receives shows what the main outlet is sending, not what customers buy.
		- Rank items by units unless the question asks for value. Quote the precomputed lists instead of adding up rows yourself.
		- Product names can carry a price tag such as "@20". Keep it exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total units and net value transferred in the period, and how many different items; " +
		"(2) the top three items by units, with their units; " +
		"(3) the category with the most units and its share as a percentage; " +
		"(4) the receiving location with the most units and the item it received most; if only one location received, say so briefly; " +
		"(5) one thing that needs attention from this data, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top 10 transferred items",
		"Which category moves the most?",
		"What does each location receive most?",
		"Which days had the most units?"
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

		var all = _transactionOverviews;
		var days = (_toDate.Date - _fromDate.Date).Days + 1;
		var totalUnits = D(all.Sum(t => t.Quantity));

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Item filter: {(_selectedProduct?.Id > 0 ? _selectedProduct.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedProductCategory?.Id > 0 ? _selectedProductCategory.Name : "All")}");
		text.AppendLine($"From-location filter: {(_selectedLocation?.Id > 0 ? _selectedLocation.Name : "All")}");
		text.AppendLine($"To-location filter: {(_selectedToLocation?.Id > 0 ? _selectedToLocation.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");

		if (_showSummary)
		{
			text.AppendLine("This is the SUMMARY view: one row per item for the whole period, so there is no location split and no day split.");
			text.AppendLine($"All items: items={all.Count}, units={N(totalUnits, "0.##")}, net={N(all.Sum(t => t.NetTotal))}, discount={N(all.Sum(t => t.DiscountAmount))}");
			text.AppendLine("Top 15 items by units: " + string.Join("; ", all.OrderByDescending(t => t.Quantity).Take(15)
				.Select(t => $"{Clean(t.ItemName)}={N(t.Quantity, "0.##")} units / {N(t.NetTotal)}")));
			text.AppendLine("Categories by units: " + string.Join("; ", all
				.GroupBy(t => t.ItemCategoryName)
				.Select(g => new { Name = g.Key, Units = D(g.Sum(t => t.Quantity)), Net = g.Sum(t => t.NetTotal) })
				.OrderByDescending(x => x.Units)
				.Select(x => $"{Clean(x.Name)}={N(x.Units, "0.##")} units ({Pct(x.Units, totalUnits)}) / {N(x.Net)}")));
			text.AppendLine();
			text.AppendLine("ITEM TABLE (CSV, whole period)");
			text.AppendLine("item,category,units,net,discount");
			foreach (var t in all.OrderByDescending(t => t.Quantity).Take(100))
				text.AppendLine(string.Join(',', Clean(t.ItemName), Clean(t.ItemCategoryName), N(t.Quantity, "0.##"), N(t.NetTotal), N(t.DiscountAmount)));

			return text.ToString();
		}

		text.AppendLine($"All transfer lines: lines={all.Count}, different items={all.Select(t => t.ItemName).Distinct().Count()}, units={N(totalUnits, "0.##")}, " +
			$"net={N(all.Sum(t => t.NetTotal))}, discount={N(all.Sum(t => t.DiscountAmount))}");

		var items = all
			.GroupBy(t => t.ItemName)
			.Select(g => new
			{
				Name = g.Key,
				Category = g.First().ItemCategoryName,
				Units = g.Sum(t => t.Quantity),
				Net = g.Sum(t => t.NetTotal),
				Discount = g.Sum(t => t.DiscountAmount),
				Locations = g.Select(t => t.ToLocationName).Distinct().Count()
			})
			.OrderByDescending(x => x.Units).ThenBy(x => x.Name)
			.ToList();

		text.AppendLine("Top 15 items by units: " + string.Join("; ", items.Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Units, "0.##")} units / {N(x.Net)} (received by {x.Locations} locations)")));
		text.AppendLine("Top 10 items by net value: " + string.Join("; ", items.OrderByDescending(x => x.Net).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Units, "0.##")} units")));
		text.AppendLine("Categories by units: " + string.Join("; ", items
			.GroupBy(x => x.Category)
			.Select(g => new { Name = g.Key, Units = D(g.Sum(x => x.Units)), Net = g.Sum(x => x.Net) })
			.OrderByDescending(x => x.Units)
			.Select(x => $"{Clean(x.Name)}={N(x.Units, "0.##")} units ({Pct(x.Units, totalUnits)}) / {N(x.Net)}")));

		text.AppendLine("By receiving location, most units first: " + string.Join("; ", all
			.GroupBy(t => t.ToLocationName)
			.Select(g => new { Name = g.Key, Units = g.Sum(t => t.Quantity), Net = g.Sum(t => t.NetTotal) })
			.OrderByDescending(x => x.Units)
			.Select(x => $"{Clean(x.Name)}={N(x.Units, "0.##")} units / {N(x.Net)}")));

		text.AppendLine("Top 3 items received at each location:");
		foreach (var location in all.GroupBy(t => t.ToLocationName).OrderBy(g => g.Key))
			text.AppendLine($"- {Clean(location.Key)}: " + string.Join("; ", location
				.GroupBy(t => t.ItemName)
				.Select(g => new { Name = g.Key, Units = g.Sum(t => t.Quantity) })
				.OrderByDescending(x => x.Units).Take(3)
				.Select(x => $"{Clean(x.Name)}={N(x.Units, "0.##")}")));

		if (days > 1)
		{
			var (grain, format) = days <= 92 ? ("Daily", "yyyy-MM-dd") : ("Monthly", "yyyy-MM");
			text.AppendLine($"{grain} units transferred: " + string.Join("; ", all
				.GroupBy(t => t.TransactionDateTime.ToString(format))
				.OrderBy(g => g.Key)
				.Select(g => $"{g.Key}={N(g.Sum(t => t.Quantity), "0.##")}")));
		}

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV, top 100 items by units)");
		text.AppendLine("item,category,units,net,discount,locations_received");

		foreach (var x in items.Take(100))
			text.AppendLine(string.Join(',',
				Clean(x.Name), Clean(x.Category), N(x.Units, "0.##"), N(x.Net), N(x.Discount), x.Locations.ToString()));

		return text.ToString();
	}
}

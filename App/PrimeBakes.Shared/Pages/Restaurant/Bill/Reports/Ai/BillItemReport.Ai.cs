using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Restaurant.Bill.Reports;

public partial class BillItemReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the BILL ITEM REPORT: the items on restaurant bills for the period. All amounts are in rupees.
		A bill is "settled" when it is no longer running, and "running" while its table is still open. Items on running bills are not final sales.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and rankings, then an ITEM TABLE (CSV) with the top 100 items by units on settled bills.
		CSV columns in the normal view: item, category, units, net (net value), discount, bills (settled bills containing the item).
		In the SUMMARY view the table is one row per item for the whole period with units, net and discount only, so there is no hour, day, location or running split; running bills are inside its totals.
		The data only contains items that were billed in the period. An item that is missing has no recorded bills; say "no recorded bills" rather than calling it a bad seller.

		HOW THE BUSINESS WORKS
		- The restaurant outlet bills tables and keeps running bills for open tables. Rankings use settled bills only; running bills are given separately.
		- Rank items by units unless the question asks for value. Quote the precomputed lists instead of adding up rows yourself.
		- Product names can carry a price tag such as "@20". Keep it exactly as written.
		- Online partner bills are orders that came through a delivery or ordering partner.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) settled units, net value and number of settled bills for the period, and the units on running bills separately (say the split is not available if the data says so); " +
		"(2) the top three items by units, with their units; " +
		"(3) the category with the highest net value and its share as a percentage; " +
		"(4) the busiest hour of the day by units, and the top online partner by units if there are any; " +
		"(5) one thing that needs attention from this data, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top 10 selling items",
		"Which category earns the most?",
		"What sells at which hour?",
		"What sells through online partners?"
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

		var all = _transactionOverviews;
		var days = (_toDate.Date - _fromDate.Date).Days + 1;

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		text.AppendLine($"Now is {_aiNow:dd-MMM-yyyy HH:mm}." + (_toDate.Date >= _aiNow.Date ? " The last day of the period is incomplete." : string.Empty));
		text.AppendLine($"Item filter: {(_selectedProduct?.Id > 0 ? _selectedProduct.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedProductCategory?.Id > 0 ? _selectedProductCategory.Name : "All")}");
		text.AppendLine($"Location filter: {(_selectedLocation?.Id > 0 ? _selectedLocation.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Customer filter: {(_selectedCustomer?.Id > 0 ? _selectedCustomer.Name : "All")}");
		text.AppendLine($"Online partner filter: {(_selectedOnlinePartner?.Id > 0 ? _selectedOnlinePartner.Name : "All")}");
		text.AppendLine($"Bills shown: {(_runningFilter == YesNoFilterOptions.Yes ? "running only" : _runningFilter == YesNoFilterOptions.No ? "settled only" : "settled and running")}. Deleted bills: {(_showDeleted ? "included" : "not included")}.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");

		if (_showSummary)
		{
			var summaryUnits = D(all.Sum(t => t.Quantity));
			text.AppendLine("This is the SUMMARY view: one row per item for the whole period, so there is no hour, day, location or running split; running bills are inside the totals.");
			text.AppendLine($"All items: items={all.Count}, units={N(summaryUnits, "0.##")}, net={N(all.Sum(t => t.NetTotal))}, discount={N(all.Sum(t => t.DiscountAmount))}");
			text.AppendLine("Top 15 items by units: " + string.Join("; ", all.OrderByDescending(t => t.Quantity).Take(15)
				.Select(t => $"{Clean(t.ItemName)}={N(t.Quantity, "0.##")} units / {N(t.NetTotal)}")));
			text.AppendLine("Categories by net value: " + string.Join("; ", all
				.GroupBy(t => t.ItemCategoryName)
				.Select(g => new { Name = g.Key, Units = g.Sum(t => t.Quantity), Net = D(g.Sum(t => t.NetTotal)) })
				.OrderByDescending(x => x.Net)
				.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Units, "0.##")} units")));
			text.AppendLine();
			text.AppendLine("ITEM TABLE (CSV, whole period)");
			text.AppendLine("item,category,units,net,discount");
			foreach (var t in all.OrderByDescending(t => t.Quantity).Take(100))
				text.AppendLine(string.Join(',', Clean(t.ItemName), Clean(t.ItemCategoryName), N(t.Quantity, "0.##"), N(t.NetTotal), N(t.DiscountAmount)));

			return text.ToString();
		}

		var settled = all.Where(t => !t.Running).ToList();
		var running = all.Where(t => t.Running).ToList();
		var settledUnits = D(settled.Sum(t => t.Quantity));
		var settledNet = D(settled.Sum(t => t.NetTotal));

		text.AppendLine($"Settled bills: lines={settled.Count}, settled bills={settled.Select(t => t.TransactionNo).Distinct().Count()}, different items={settled.Select(t => t.ItemName).Distinct().Count()}, " +
			$"units={N(settledUnits, "0.##")}, net={N(settledNet)}, discount={N(settled.Sum(t => t.DiscountAmount))}");
		text.AppendLine($"Running bills (open tables, not final): lines={running.Count}, running bills={running.Select(t => t.TransactionNo).Distinct().Count()}, units={N(running.Sum(t => t.Quantity), "0.##")}, net so far={N(running.Sum(t => t.NetTotal))}");

		var items = settled
			.GroupBy(t => t.ItemName)
			.Select(g => new
			{
				Name = g.Key,
				Category = g.First().ItemCategoryName,
				Units = g.Sum(t => t.Quantity),
				Net = g.Sum(t => t.NetTotal),
				Discount = g.Sum(t => t.DiscountAmount),
				Bills = g.Select(t => t.TransactionNo).Distinct().Count()
			})
			.OrderByDescending(x => x.Units).ThenBy(x => x.Name)
			.ToList();

		text.AppendLine("Top 15 items by units: " + string.Join("; ", items.Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Units, "0.##")} units / {N(x.Net)} in {x.Bills} bills")));
		text.AppendLine("Top 10 items by net value: " + string.Join("; ", items.OrderByDescending(x => x.Net).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Units, "0.##")} units")));
		text.AppendLine("Categories by net value: " + string.Join("; ", items
			.GroupBy(x => x.Category)
			.Select(g => new { Name = g.Key, Units = g.Sum(x => x.Units), Net = D(g.Sum(x => x.Net)) })
			.OrderByDescending(x => x.Net)
			.Select(x => $"{Clean(x.Name)}={N(x.Net)} ({Pct(x.Net, settledNet)}) / {N(x.Units, "0.##")} units")));

		text.AppendLine("By hour of day (settled units / net): " + string.Join("; ", settled
			.GroupBy(t => t.TransactionDateTime.Hour)
			.OrderBy(g => g.Key)
			.Select(g => $"{g.Key:00}:00={N(g.Sum(t => t.Quantity), "0.##")} / {N(g.Sum(t => t.NetTotal))}")));

		text.AppendLine("Online partner lines: " + string.Join("; ", settled
			.Where(t => !string.IsNullOrWhiteSpace(t.OnlinePartnerName))
			.GroupBy(t => t.OnlinePartnerName)
			.Select(g => new { Name = g.Key, Units = g.Sum(t => t.Quantity), Net = g.Sum(t => t.NetTotal) })
			.OrderByDescending(x => x.Net)
			.Select(x => $"{Clean(x.Name)}={N(x.Units, "0.##")} units / {N(x.Net)}")));

		if (settled.Select(t => t.LocationName).Distinct().Count() > 1)
			text.AppendLine("By location (settled units / net): " + string.Join("; ", settled
				.GroupBy(t => t.LocationName)
				.Select(g => new { Name = g.Key, Units = g.Sum(t => t.Quantity), Net = g.Sum(t => t.NetTotal) })
				.OrderByDescending(x => x.Net)
				.Select(x => $"{Clean(x.Name)}={N(x.Units, "0.##")} / {N(x.Net)}")));

		if (days > 1)
		{
			var (grain, format) = days <= 92 ? ("Daily", "yyyy-MM-dd") : ("Monthly", "yyyy-MM");
			text.AppendLine($"{grain} settled units: " + string.Join("; ", settled
				.GroupBy(t => t.TransactionDateTime.ToString(format))
				.OrderBy(g => g.Key)
				.Select(g => $"{g.Key}={N(g.Sum(t => t.Quantity), "0.##")}")));
		}

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV, settled bills, top 100 items by units)");
		text.AppendLine("item,category,units,net,discount,bills");

		foreach (var x in items.Take(100))
			text.AppendLine(string.Join(',', Clean(x.Name), Clean(x.Category), N(x.Units, "0.##"), N(x.Net), N(x.Discount), x.Bills.ToString()));

		return text.ToString();
	}
}

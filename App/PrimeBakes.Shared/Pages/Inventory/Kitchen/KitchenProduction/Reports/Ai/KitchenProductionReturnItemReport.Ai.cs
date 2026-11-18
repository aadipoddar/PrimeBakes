using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen.KitchenProduction.Reports;

public partial class KitchenProductionReturnItemReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the KITCHEN PRODUCTION RETURN ITEM REPORT: finished products coming back from the kitchens, broken down by product. REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and rankings, then an ITEM TABLE (CSV).
		CSV columns: item, category, units (quantity returned), value (rupee value at the rates recorded).
		This report has returns only. It does not have the production they came from, so do not give a return rate or a percentage of production.
		The data only contains products that have recorded returns in the period. A missing product had no recorded returns, which is fine.
		In the whole-period summary view there is one row per product with no kitchen, transaction or day split. If asked for those, say that view does not have them.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) buys raw material, issues it to the kitchen, and the kitchen makes the finished products. A kitchen production return is finished products coming back, which the business treats as damage. Never guess the cause of a return.
		- Value is what the products are valued at when recorded, not sales. Never call it revenue or sales.
		- Rank products by units unless the question asks for value. Quote units and value from the precomputed lists.
		- Product names can carry a price tag such as "@20". Keep it exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total units returned, their value and the number of returns (if the data is the summary view, leave out the number of returns and say it is not available); " +
		"(2) the top three products by units returned, with their units; " +
		"(3) the category with the highest returned value and its share of the total as a percentage; " +
		"(4) the kitchen with the most units returned, with its units (if the data is the summary view, say it is not available); " +
		"(5) one thing that needs attention from this data, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top 10 returned products",
		"Which category is returned the most?",
		"Which kitchen returned the most?",
		"Show returns by day"
	];

	private bool _showAi = false;
	private DateTime _aiToday = DateTime.Now.Date;

	private async Task ToggleAi()
	{
		_showAi = !_showAi;
		if (_showAi)
			_aiToday = (await CommonData.LoadCurrentDateTime()).Date;
	}

	// Text version of what is on screen (_transactionOverviews already has every filter applied).
	// Rankings and totals are computed here, because the model is unreliable at adding up hundreds of rows.
	// The item table has one row per product, so its size depends on the product count, not the period.
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
		text.AppendLine($"Product filter: {(_selectedProduct?.Id > 0 ? _selectedProduct.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedProductCategory?.Id > 0 ? _selectedProductCategory.Name : "All")}");
		text.AppendLine($"Kitchen filter: {(_selectedKitchen?.Id > 0 ? _selectedKitchen.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		if (_showSummary)
			text.AppendLine("This is the SUMMARY view: one row per product for the whole period, so there is no split by kitchen, transaction or day.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All returns: {(_showSummary ? string.Empty : $"returns={all.Select(t => t.TransactionNo).Distinct().Count()}, ")}" +
			$"units={N(all.Sum(t => t.Quantity), "0.##")}, value={N(all.Sum(t => t.Total))}");

		var items = all
			.GroupBy(t => t.ItemName)
			.Select(g => new
			{
				Name = g.Key,
				Qty = g.Sum(t => t.Quantity),
				Value = g.Sum(t => t.Total)
			})
			.ToList();

		text.AppendLine("Top 15 products by returned units: " + string.Join("; ", items.OrderByDescending(x => x.Qty).Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Qty, "0.##")} units / {N(x.Value)}")));
		text.AppendLine("Top 10 products by returned value: " + string.Join("; ", items.OrderByDescending(x => x.Value).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Value)} / {N(x.Qty, "0.##")} units")));
		text.AppendLine("Categories by returned value: " + string.Join("; ", all
			.GroupBy(t => t.ItemCategoryName)
			.Select(g => new { Name = g.Key, Qty = g.Sum(t => t.Quantity), Value = g.Sum(t => t.Total) })
			.OrderByDescending(x => x.Value)
			.Select(x => $"{Clean(x.Name)}={N(x.Value)} / {N(x.Qty, "0.##")} units")));

		if (!_showSummary)
		{
			text.AppendLine("Returned units by kitchen, most first: " + string.Join("; ", all
				.GroupBy(t => t.KitchenName)
				.Select(g => new { Name = g.Key, Qty = g.Sum(t => t.Quantity), Value = g.Sum(t => t.Total), Returns = g.Select(t => t.TransactionNo).Distinct().Count() })
				.OrderByDescending(x => x.Qty)
				.Select(x => $"{Clean(x.Name)}={N(x.Qty, "0.##")} units / {N(x.Value)} ({x.Returns} returns)")));

			text.AppendLine("Top 3 products by returned units at each kitchen:");
			foreach (var kitchen in all.GroupBy(t => t.KitchenName).OrderBy(g => g.Key))
				text.AppendLine($"- {Clean(kitchen.Key)}: " + string.Join("; ", kitchen
					.GroupBy(t => t.ItemName)
					.Select(g => new { Name = g.Key, Qty = g.Sum(t => t.Quantity) })
					.OrderByDescending(x => x.Qty).Take(3)
					.Select(x => $"{Clean(x.Name)}={N(x.Qty, "0.##")}")));

			if (days > 1)
			{
				var (grain, format) = days <= 92 ? ("Daily", "dd-MMM-yyyy") : ("Monthly", "MMM-yyyy");
				text.AppendLine($"{grain} returns (units / value): " + string.Join("; ", all
					.GroupBy(t => t.TransactionDateTime.ToString(days <= 92 ? "yyyy-MM-dd" : "yyyy-MM"))
					.OrderBy(g => g.Key)
					.Select(g => $"{g.First().TransactionDateTime.ToString(format)}={N(g.Sum(t => t.Quantity), "0.##")} / {N(g.Sum(t => t.Total))}")));
			}
		}

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV, whole period)");
		text.AppendLine("item,category,units,value");

		var rows = all
			.GroupBy(t => (Item: t.ItemName, Category: t.ItemCategoryName))
			.OrderBy(g => g.Key.Category).ThenBy(g => g.Key.Item);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				Clean(g.Key.Item),
				Clean(g.Key.Category),
				N(g.Sum(t => t.Quantity), "0.##"),
				N(g.Sum(t => t.Total))));

		return text.ToString();
	}
}

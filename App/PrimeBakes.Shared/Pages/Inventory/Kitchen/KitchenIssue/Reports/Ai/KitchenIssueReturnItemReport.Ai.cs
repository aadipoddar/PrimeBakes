using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen.KitchenIssue.Reports;

public partial class KitchenIssueReturnItemReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the KITCHEN ISSUE RETURN ITEM REPORT: raw material coming back from the kitchens to the main store, broken down by raw material. REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and rankings, then an ITEM TABLE (CSV).
		CSV columns: item, category, uom (unit of measurement, can be blank), qty (quantity in that unit), value (rupee value).
		Raw materials are measured in different units (kg, litres, pieces), so never add up or compare quantities across different items. Rank and compare items by rupee value. Give an item's quantity only together with its own unit. If a unit is blank, do not guess one.
		This report has returns only. It does not have the issues they came from, so do not give a return rate or a percentage of issues.
		The data only contains raw materials that have recorded returns in the period. A missing item had no recorded returns, which is fine.
		In the whole-period summary view there is one row per item with no kitchen, transaction or day split. If asked for those, say that view does not have them.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) buys raw material and issues it to the kitchen, where the finished products are made. A kitchen issue return is raw material coming back from the kitchen, which the business treats as damage. Never guess the cause of a return.
		- Value is the cost of raw material, not sales. Never call it revenue or sales.
		- Item names can carry extra text such as a size or price tag. Keep names exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total returned value and the number of returns (if the data is the summary view, leave out the number of returns and say it is not available); " +
		"(2) the top three raw materials by returned value, with their value and quantity in their own unit; " +
		"(3) the category with the highest returned value and its share of the total as a percentage; " +
		"(4) the kitchen with the highest returned value, with its value (if the data is the summary view, say it is not available); " +
		"(5) one thing that needs attention from this data, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top 10 returned raw materials",
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
	// The item table has one row per item, so its size depends on the item count, not the period.
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
		text.AppendLine($"Raw material filter: {(_selectedRawMaterial?.Id > 0 ? _selectedRawMaterial.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedRawMaterialCategory?.Id > 0 ? _selectedRawMaterialCategory.Name : "All")}");
		text.AppendLine($"Kitchen filter: {(_selectedKitchen?.Id > 0 ? _selectedKitchen.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		if (_showSummary)
			text.AppendLine("This is the SUMMARY view: one row per raw material for the whole period, so there is no split by kitchen, transaction or day.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All returns: {(_showSummary ? string.Empty : $"returns={all.Select(t => t.TransactionNo).Distinct().Count()}, ")}value={N(all.Sum(t => t.Total))}");

		var items = all
			.GroupBy(t => t.ItemName)
			.Select(g => new
			{
				Name = g.Key,
				Uom = g.First().UnitOfMeasurement,
				Qty = g.Sum(t => t.Quantity),
				Value = g.Sum(t => t.Total)
			})
			.ToList();

		text.AppendLine("Top 15 raw materials by returned value: " + string.Join("; ", items.OrderByDescending(x => x.Value).Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Value)} / {N(x.Qty, "0.##")} {Clean(x.Uom)}")));
		text.AppendLine("Categories by returned value: " + string.Join("; ", all
			.GroupBy(t => t.ItemCategoryName)
			.Select(g => new { Name = g.Key, Value = g.Sum(t => t.Total) })
			.OrderByDescending(x => x.Value)
			.Select(x => $"{Clean(x.Name)}={N(x.Value)}")));

		if (!_showSummary)
		{
			text.AppendLine("Returned value by kitchen, highest first: " + string.Join("; ", all
				.GroupBy(t => t.KitchenName)
				.Select(g => new { Name = g.Key, Value = g.Sum(t => t.Total), Returns = g.Select(t => t.TransactionNo).Distinct().Count() })
				.OrderByDescending(x => x.Value)
				.Select(x => $"{Clean(x.Name)}={N(x.Value)} ({x.Returns} returns)")));

			text.AppendLine("Top 3 raw materials by returned value at each kitchen:");
			foreach (var kitchen in all.GroupBy(t => t.KitchenName).OrderBy(g => g.Key))
				text.AppendLine($"- {Clean(kitchen.Key)}: " + string.Join("; ", kitchen
					.GroupBy(t => t.ItemName)
					.Select(g => new { Name = g.Key, Uom = g.First().UnitOfMeasurement, Qty = g.Sum(t => t.Quantity), Value = g.Sum(t => t.Total) })
					.OrderByDescending(x => x.Value).Take(3)
					.Select(x => $"{Clean(x.Name)}={N(x.Value)} / {N(x.Qty, "0.##")} {Clean(x.Uom)}")));

			if (days > 1)
			{
				var (grain, format) = days <= 92 ? ("Daily", "dd-MMM-yyyy") : ("Monthly", "MMM-yyyy");
				text.AppendLine($"{grain} returned value: " + string.Join("; ", all
					.GroupBy(t => t.TransactionDateTime.ToString(days <= 92 ? "yyyy-MM-dd" : "yyyy-MM"))
					.OrderBy(g => g.Key)
					.Select(g => $"{g.First().TransactionDateTime.ToString(format)}={N(g.Sum(t => t.Total))}")));
			}
		}

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV, whole period)");
		text.AppendLine("item,category,uom,qty,value");

		var rows = all
			.GroupBy(t => (Item: t.ItemName, Category: t.ItemCategoryName))
			.OrderBy(g => g.Key.Category).ThenBy(g => g.Key.Item);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				Clean(g.Key.Item),
				Clean(g.Key.Category),
				Clean(g.First().UnitOfMeasurement),
				N(g.Sum(t => t.Quantity), "0.##"),
				N(g.Sum(t => t.Total))));

		return text.ToString();
	}
}

using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Kitchen.KitchenIssue.Reports;

public partial class KitchenIssueItemReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the KITCHEN ISSUE ITEM REPORT: raw material issued from the main store to the kitchens, broken down by raw material. REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and rankings, then an ITEM TABLE (CSV).
		CSV columns: item, category, uom (unit of measurement, can be blank), qty (quantity in that unit), value (rupee value).
		Raw materials are measured in different units (kg, litres, pieces), so never add up or compare quantities across different items. Rank and compare items by rupee value. Give an item's quantity only together with its own unit. If a unit is blank, do not guess one.
		Negative quantities and values are kitchen issue returns. Returns are present only when the data says they are included; if it says returns are not included, say so and give no returns figure.
		The data only contains raw materials that have recorded issues in the period. A missing item had no recorded issues; do not call it unused, just say it has no recorded issues.
		In the whole-period summary view there is one row per item with no kitchen, transaction or day split. If asked for those, say that view does not have them.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) buys raw material and issues it to the kitchen, where the finished products are made. A kitchen issue is raw material going to a kitchen. A kitchen issue return is raw material coming back from the kitchen, which the business treats as damage. Never guess the cause of a return.
		- Issue value is the cost of raw material, not sales. Never call it revenue or sales.
		- Item names can carry extra text such as a size or price tag. Keep names exactly as written.
		- Low or missing issues for a kitchen mean "recorded issues", not that the kitchen is idle.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total issue value and the number of issues, then returns (say they are not included if the data says so); if the data is the summary view, leave out the number of issues and say it is not available; " +
		"(2) the top three raw materials by value, with their value and quantity in their own unit; " +
		"(3) the category with the highest value and its share of the total issue value as a percentage; " +
		"(4) the three lowest-value raw materials among those with recorded issues, with their value; " +
		"(5) one thing that needs attention from this data, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top 10 raw materials by value",
		"Which category costs the most?",
		"Lowest value raw materials",
		"Top raw materials at each kitchen"
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
		static bool IsReturn(KitchenIssueItemOverviewModel t) => t.Quantity < 0 || t.Total < 0;

		var all = _transactionOverviews;
		var days = (_toDate.Date - _fromDate.Date).Days + 1;

		// The summary view is already netted per item, so issues and returns cannot be told apart there
		var issueRows = _showSummary ? all : all.Where(t => !IsReturn(t)).ToList();
		var returnRows = _showSummary ? new List<KitchenIssueItemOverviewModel>() : all.Where(IsReturn).ToList();

		// TransactionNo is used to count transactions because issue and return ids can overlap
		string Totals(List<KitchenIssueItemOverviewModel> rows) =>
			$"{(_showSummary ? string.Empty : $"issues={rows.Select(t => t.TransactionNo).Distinct().Count()}, ")}value={N(rows.Sum(t => t.Total))}";

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Raw material filter: {(_selectedRawMaterial?.Id > 0 ? _selectedRawMaterial.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedRawMaterialCategory?.Id > 0 ? _selectedRawMaterialCategory.Name : "All")}");
		text.AppendLine($"Kitchen filter: {(_selectedKitchen?.Id > 0 ? _selectedKitchen.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		text.AppendLine(!_showTransactionReturns ? "Kitchen issue returns: NOT included in this data, so there is no returns figure."
			: _showSummary ? "Kitchen issue returns: included, netted into the quantities and values."
			: $"Kitchen issue returns: included as negative rows; {returnRows.Select(t => t.TransactionNo).Distinct().Count()} returns worth {N(-returnRows.Sum(t => t.Total))} in total.");

		if (_showSummary)
			text.AppendLine("This is the SUMMARY view: one row per raw material for the whole period, so there is no split by kitchen, transaction or day.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Issues: {Totals(issueRows)}");
		if (_showTransactionReturns && !_showSummary)
			text.AppendLine($"Net of returns: value={N(all.Sum(t => t.Total))}");

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

		text.AppendLine("Top 15 raw materials by value: " + string.Join("; ", items.OrderByDescending(x => x.Value).Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Value)} / {N(x.Qty, "0.##")} {Clean(x.Uom)}")));
		text.AppendLine("Lowest 10 raw materials by value (only items with recorded issues): " + string.Join("; ", items.Where(x => x.Value > 0).OrderBy(x => x.Value).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Value)} / {N(x.Qty, "0.##")} {Clean(x.Uom)}")));
		text.AppendLine("Categories by value: " + string.Join("; ", all
			.GroupBy(t => t.ItemCategoryName)
			.Select(g => new { Name = g.Key, Value = g.Sum(t => t.Total) })
			.OrderByDescending(x => x.Value)
			.Select(x => $"{Clean(x.Name)}={N(x.Value)}")));

		if (!_showSummary)
		{
			text.AppendLine("Top 3 raw materials by value at each kitchen:");
			foreach (var kitchen in all.GroupBy(t => t.KitchenName).OrderBy(g => g.Key))
				text.AppendLine($"- {Clean(kitchen.Key)} (total {N(kitchen.Sum(t => t.Total))}): " + string.Join("; ", kitchen
					.GroupBy(t => t.ItemName)
					.Select(g => new { Name = g.Key, Uom = g.First().UnitOfMeasurement, Qty = g.Sum(t => t.Quantity), Value = g.Sum(t => t.Total) })
					.OrderByDescending(x => x.Value).Take(3)
					.Select(x => $"{Clean(x.Name)}={N(x.Value)} / {N(x.Qty, "0.##")} {Clean(x.Uom)}")));

			if (days > 1)
			{
				var (grain, format) = days <= 92 ? ("Daily", "dd-MMM-yyyy") : ("Monthly", "MMM-yyyy");
				text.AppendLine($"{grain} issue value: " + string.Join("; ", all
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

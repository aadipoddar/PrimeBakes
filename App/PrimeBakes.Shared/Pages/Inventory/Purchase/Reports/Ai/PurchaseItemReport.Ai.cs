using PrimeBakes.Models.Inventory.Purchase;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Purchase.Reports;

public partial class PurchaseItemReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the PURCHASE ITEM REPORT: raw material bought from suppliers (the "party" on each purchase), broken down by raw material. REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and rankings, then an ITEM TABLE (CSV).
		CSV columns: item, category, uom (unit of measurement, can be blank), qty (quantity in that unit), net (net value in rupees, tax included), avg_rate (net divided by qty, per unit, can be blank).
		Raw materials are measured in different units (kg, litres, pieces), so never add up or compare quantities across different items. Rank and compare items by net value. Give an item's quantity only together with its own unit. If a unit is blank, do not guess one.
		Negative quantities and values are purchase returns, meaning raw material sent back to the supplier. Returns are present only when the data says they are included; if it says returns are not included, say so and give no returns figure.
		The data only contains raw materials that have recorded purchases in the period. A missing item had no recorded purchases; do not call it unused.
		In the whole-period summary view there is one row per item with no supplier, purchase or day split and no price comparison between suppliers. If asked for those, say that view does not have them.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) buys raw material from suppliers and issues it to the kitchen. Purchase value is a cost, not sales. Never call it revenue or sales.
		- avg_rate is only the net value divided by the quantity, so it includes tax and item discounts. Compare rates between suppliers only for the same item and say that the difference is in the recorded rate. Never call a rate a mistake or overcharging.
		- Item names can carry extra text such as a size. Keep names exactly as written.
		- Never guess why a purchase or a return was made.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total purchase net value and the number of purchases, then returns (say they are not included if the data says so); if the data is the summary view, leave out the number of purchases and say it is not available; " +
		"(2) the top three raw materials by net value, with their value and quantity in their own unit; " +
		"(3) the category with the highest net value and its share of the total as a percentage; " +
		"(4) the supplier with the highest net value and its share (if the data is the summary view, say it is not available); " +
		"(5) one thing that needs attention from this data, such as a big rate difference between suppliers for the same item, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top 10 raw materials by value",
		"Compare rates between suppliers",
		"Which category costs the most?",
		"Top raw materials from each supplier"
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
		static string Rate(double net, double qty) => qty == 0 ? string.Empty : (net / qty).ToString("0.##", CultureInfo.InvariantCulture);
		static bool IsReturn(PurchaseItemOverviewModel t) => t.Quantity < 0 || t.Total < 0;

		var all = _transactionOverviews;
		var days = (_toDate.Date - _fromDate.Date).Days + 1;

		// The summary view is already netted per item, so purchases and returns cannot be told apart there
		var purchaseRows = _showSummary ? all : all.Where(t => !IsReturn(t)).ToList();
		var returnRows = _showSummary ? new List<PurchaseItemOverviewModel>() : all.Where(IsReturn).ToList();

		var grand = Convert.ToDouble(all.Sum(t => t.NetTotal));
		string Share(double part) => grand == 0 ? "n/a" : $"{(part * 100 / grand).ToString("0.#", CultureInfo.InvariantCulture)}%";

		// TransactionNo is used to count transactions because purchase and return ids can overlap
		string Totals(List<PurchaseItemOverviewModel> rows) =>
			$"{(_showSummary ? string.Empty : $"purchases={rows.Select(t => t.TransactionNo).Distinct().Count()}, ")}" +
			$"net={N(rows.Sum(t => t.NetTotal))}, discount={N(rows.Sum(t => t.DiscountAmount))}";

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Raw material filter: {(_selectedRawMaterial?.Id > 0 ? _selectedRawMaterial.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedRawMaterialCategory?.Id > 0 ? _selectedRawMaterialCategory.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Party filter: {(_selectedParty?.Id > 0 ? _selectedParty.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		text.AppendLine(!_showTransactionReturns ? "Purchase returns: NOT included in this data, so there is no returns figure."
			: _showSummary ? "Purchase returns: included, netted into the quantities and values."
			: $"Purchase returns: included as negative rows; {returnRows.Select(t => t.TransactionNo).Distinct().Count()} returns worth {N(-returnRows.Sum(t => t.NetTotal))} in total.");

		if (_showSummary)
			text.AppendLine("This is the SUMMARY view: one row per raw material for the whole period, so there is no split by supplier, purchase or day.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Purchases: {Totals(purchaseRows)}");
		if (_showTransactionReturns && !_showSummary)
			text.AppendLine($"Net of returns: net={N(all.Sum(t => t.NetTotal))}");

		var items = all
			.GroupBy(t => t.ItemName)
			.Select(g => new
			{
				Name = g.Key,
				Uom = g.First().UnitOfMeasurement,
				Qty = Convert.ToDouble(g.Sum(t => t.Quantity)),
				Net = Convert.ToDouble(g.Sum(t => t.NetTotal))
			})
			.ToList();

		text.AppendLine("Top 15 raw materials by net value: " + string.Join("; ", items.OrderByDescending(x => x.Net).Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Qty, "0.##")} {Clean(x.Uom)} / avg rate {Rate(x.Net, x.Qty)}")));
		text.AppendLine("Lowest 10 raw materials by net value (only items with recorded purchases): " + string.Join("; ", items.Where(x => x.Net > 0).OrderBy(x => x.Net).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Qty, "0.##")} {Clean(x.Uom)}")));
		text.AppendLine("Categories by net value: " + string.Join("; ", all
			.GroupBy(t => t.ItemCategoryName)
			.Select(g => new { Name = g.Key, Net = Convert.ToDouble(g.Sum(t => t.NetTotal)) })
			.OrderByDescending(x => x.Net)
			.Select(x => $"{Clean(x.Name)}={N(x.Net)} (share {Share(x.Net)})")));

		if (!_showSummary)
		{
			text.AppendLine("Top 10 suppliers by net value: " + string.Join("; ", all
				.GroupBy(t => t.PartyName)
				.Select(g => new { Name = g.Key, Net = Convert.ToDouble(g.Sum(t => t.NetTotal)), Purchases = g.Select(t => t.TransactionNo).Distinct().Count() })
				.OrderByDescending(x => x.Net).Take(10)
				.Select(x => $"{Clean(x.Name)}={N(x.Net)} (share {Share(x.Net)}, {x.Purchases} transactions)")));

			text.AppendLine("Rate by supplier for the top 5 raw materials by net value (net value divided by quantity, per unit):");
			foreach (var item in items.OrderByDescending(x => x.Net).Take(5))
				text.AppendLine($"- {Clean(item.Name)} ({Clean(item.Uom)}): " + string.Join("; ", all
					.Where(t => t.ItemName == item.Name)
					.GroupBy(t => t.PartyName)
					.Select(g => (Party: g.Key, Qty: Convert.ToDouble(g.Sum(t => t.Quantity)), Net: Convert.ToDouble(g.Sum(t => t.NetTotal))))
					.OrderByDescending(x => x.Net).Take(5)
					.Select(x => $"{Clean(x.Party)}={Rate(x.Net, x.Qty)} for {N(x.Qty, "0.##")}")));

			text.AppendLine("Top 3 raw materials by net value from each supplier (top 10 suppliers):");
			foreach (var party in all.GroupBy(t => t.PartyName).OrderByDescending(g => g.Sum(t => t.NetTotal)).Take(10))
				text.AppendLine($"- {Clean(party.Key)}: " + string.Join("; ", party
					.GroupBy(t => t.ItemName)
					.Select(g => new { Name = g.Key, Uom = g.First().UnitOfMeasurement, Qty = Convert.ToDouble(g.Sum(t => t.Quantity)), Net = Convert.ToDouble(g.Sum(t => t.NetTotal)) })
					.OrderByDescending(x => x.Net).Take(3)
					.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Qty, "0.##")} {Clean(x.Uom)}")));

			if (days > 1)
			{
				var (grain, format) = days <= 92 ? ("Daily", "dd-MMM-yyyy") : ("Monthly", "MMM-yyyy");
				text.AppendLine($"{grain} purchase net value: " + string.Join("; ", all
					.GroupBy(t => t.TransactionDateTime.ToString(days <= 92 ? "yyyy-MM-dd" : "yyyy-MM"))
					.OrderBy(g => g.Key)
					.Select(g => $"{g.First().TransactionDateTime.ToString(format)}={N(g.Sum(t => t.NetTotal))}")));
			}
		}

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV, whole period)");
		text.AppendLine("item,category,uom,qty,net,avg_rate");

		var rows = all
			.GroupBy(t => (Item: t.ItemName, Category: t.ItemCategoryName))
			.OrderBy(g => g.Key.Category).ThenBy(g => g.Key.Item);

		foreach (var g in rows)
		{
			var qty = Convert.ToDouble(g.Sum(t => t.Quantity));
			var net = Convert.ToDouble(g.Sum(t => t.NetTotal));
			text.AppendLine(string.Join(',',
				Clean(g.Key.Item),
				Clean(g.Key.Category),
				Clean(g.First().UnitOfMeasurement),
				N(qty, "0.##"),
				N(net),
				Rate(net, qty)));
		}

		return text.ToString();
	}
}

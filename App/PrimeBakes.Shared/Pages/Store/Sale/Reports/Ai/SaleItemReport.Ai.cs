using PrimeBakes.Models.Store.Sale;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Sale.Reports;

public partial class SaleItemReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the SALE ITEM REPORT: sales broken down by item (product). REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and rankings, then an ITEM TABLE (CSV).
		CSV columns: item, category, type, units (quantity sold), net (net value), discount.
		type is "supply" for the main outlet's internal supply to other outlets, "outlet" for every other location, or "all" in the summary view where the split is not available.
		Negative units and values are sale returns. Returns, stock transfers and restaurant bills are present only when the data says they are included; if it says returns are not included, say so and give no returns figure.
		The data only contains items that have recorded sales in the period. An item that is missing was not sold; do not call it a bad seller, just say it has no recorded sales.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It supplies products to the other outlets on credit. That is internal supply, not customer sales. Its discounts are part of the supply terms to outlets, so do not present them as a concern.
		- For any question about best sellers, slow movers, categories or what customers buy, use the outlet figures only and say that supply is left out. Mention supply separately and briefly, only if it helps, for example what the outlets are ordering from head office.
		- Rank items by units unless the question asks for value. Quote units and net value from the precomputed lists.
		- Product names can carry a price tag such as "@20". Keep it exactly as written.
		- Outlet sales reach the software in three ways: some outlets bill live, some have the whole day's sales entered at the end of the day, and for some only the remaining amount is handed over to the main outlet. So low or missing sales at an outlet are NOT necessarily weak performance. Say "recorded sales".
		- Returns are either damaged stock sent back to the main outlet or customers returning items. The data does not say which, so call them "returns" and never assume a cause.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) outlet units and net sales for the period, then the main outlet's supply units, then returns (say they are not included if the data says so); " +
		"(2) the top three items by units, with their units; " +
		"(3) the category with the highest net value and its share of outlet net sales as a percentage; " +
		"(4) the three slowest items among those with recorded sales, with their units; " +
		"(5) one thing that needs attention from this data, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top 10 selling items",
		"Which category earns the most?",
		"Slowest moving items",
		"Best items at each outlet"
	];

	private const int MainOutletLocationId = 1;

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
	// The item table has one row per item (and per outlet/supply), so its size depends on the item count, not the period.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static bool IsSupply(SaleItemOverviewModel t) => t.LocationId == MainOutletLocationId;

		string Totals(List<SaleItemOverviewModel> rows) =>
			$"units={N(rows.Sum(t => t.Quantity), "0.##")}, net={N(rows.Sum(t => t.NetTotal))}, discount={N(rows.Sum(t => t.DiscountAmount))}";

		var all = _transactionOverviews;

		// The summary view has no location or date, so the outlet / supply split is not possible there
		var outlets = _showSummary ? all : all.Where(t => !IsSupply(t)).ToList();
		var supply = _showSummary ? new List<SaleItemOverviewModel>() : all.Where(IsSupply).ToList();

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
		text.AppendLine($"Stock transfers: {(_showStockTransfers ? "included" : "not included")}. Restaurant bills: {(_showBills ? "included" : "not included")}. Deleted transactions: {(_showDeleted ? "included" : "not included")}.");
		text.AppendLine($"Coco outlets: {(_showCoco ? "shown" : "hidden")}. Fofo outlets: {(_showFofo ? "shown" : "hidden")}.");

		var returnRows = all.Where(t => t.Quantity < 0).ToList();
		text.AppendLine(!_showSaleReturns ? "Sale returns: NOT included in this data, so there is no returns figure."
			: _showSummary ? "Sale returns: included, netted into the totals."
			: $"Sale returns: included as negative rows; {N(-returnRows.Sum(t => t.Quantity), "0.##")} units, net value {(returnRows.Sum(t => t.NetTotal) == 0 ? "not recorded (0)" : N(-returnRows.Sum(t => t.NetTotal)))}.");

		if (_showSummary)
			text.AppendLine("This is the SUMMARY view: one row per item for the whole period, so there is no split by outlet, location or day.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"{(_showSummary ? "All rows" : "Outlet sales (all outlets except the main outlet)")}: {Totals(outlets)}");
		if (!_showSummary)
		{
			text.AppendLine($"Main outlet internal supply (PRIME BAKES OFFICE): {Totals(supply)}");
			text.AppendLine($"All rows together: {Totals(all)}");
		}

		var items = outlets
			.GroupBy(t => t.ItemName)
			.Select(g => new
			{
				Name = g.Key,
				Qty = g.Sum(t => t.Quantity),
				Net = g.Sum(t => t.NetTotal)
			})
			.ToList();

		text.AppendLine("Top 15 items by units: " + string.Join("; ", items.OrderByDescending(x => x.Qty).Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Qty, "0.##")} units / {N(x.Net)}")));
		text.AppendLine("Top 10 items by net value: " + string.Join("; ", items.OrderByDescending(x => x.Net).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Qty, "0.##")} units")));
		text.AppendLine("Slowest 10 items by units (only items with recorded sales): " + string.Join("; ", items.Where(x => x.Qty > 0).OrderBy(x => x.Qty).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Qty, "0.##")}")));
		text.AppendLine("Categories by net value: " + string.Join("; ", outlets
			.GroupBy(t => t.ItemCategoryName)
			.Select(g => new { Name = g.Key, Qty = g.Sum(t => t.Quantity), Net = g.Sum(t => t.NetTotal) })
			.OrderByDescending(x => x.Net)
			.Select(x => $"{Clean(x.Name)}={N(x.Net)} / {N(x.Qty, "0.##")} units")));

		if (!_showSummary)
		{
			text.AppendLine("Top 3 items by units at each outlet:");
			foreach (var location in outlets.GroupBy(t => t.LocationName).OrderBy(g => g.Key))
				text.AppendLine($"- {Clean(location.Key)}: " + string.Join("; ", location
					.GroupBy(t => t.ItemName)
					.Select(g => new { Name = g.Key, Qty = g.Sum(t => t.Quantity) })
					.OrderByDescending(x => x.Qty).Take(3)
					.Select(x => $"{Clean(x.Name)}={N(x.Qty, "0.##")}")));

			if (days > 1)
			{
				var (grain, format) = days <= 92 ? ("Daily", "dd-MMM-yyyy") : ("Monthly", "MMM-yyyy");
				text.AppendLine($"{grain} outlet sales (units / net value): " + string.Join("; ", outlets
					.GroupBy(t => t.TransactionDateTime.ToString(days <= 92 ? "yyyy-MM-dd" : "yyyy-MM"))
					.OrderBy(g => g.Key)
					.Select(g => $"{g.First().TransactionDateTime.ToString(format)}={N(g.Sum(t => t.Quantity), "0.##")} / {N(g.Sum(t => t.NetTotal))}")));
			}
		}

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV, whole period)");
		text.AppendLine("item,category,type,units,net,discount");

		var rows = all
			.GroupBy(t => (Item: t.ItemName, Category: t.ItemCategoryName, Supply: !_showSummary && IsSupply(t)))
			.OrderBy(g => g.Key.Category).ThenBy(g => g.Key.Item);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				Clean(g.Key.Item),
				Clean(g.Key.Category),
				_showSummary ? "all" : g.Key.Supply ? "supply" : "outlet",
				N(g.Sum(t => t.Quantity), "0.##"),
				N(g.Sum(t => t.NetTotal)),
				N(g.Sum(t => t.DiscountAmount))));

		return text.ToString();
	}
}

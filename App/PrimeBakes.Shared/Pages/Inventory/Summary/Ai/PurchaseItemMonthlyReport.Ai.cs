using PrimeBakes.Models.Inventory.Purchase;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Summary;

public partial class PurchaseItemMonthlyReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the PURCHASE ITEM MONTHLY REPORT: raw material bought from suppliers, one row per raw material, with one column per month of one financial year. REPORT DATA has the financial year, filters and what the month columns show, then PRECOMPUTED TOTALS and lists, then an ITEM TABLE (CSV).
		The month columns show either quantity (in each item's own unit) or net rupee value (tax included); the data says which. Raw materials are measured in different units (kg, litres, pieces), so never add up or compare quantities across different items. Rank and compare items by net value. Give an item's quantity only together with its own unit.
		ITEM TABLE columns: item, category, uom, one column per month (in what the month columns show), total_in_measure (the 12 months added up), net_value (net purchase value in rupees for the year), avg_rate (average net rate per unit). Only the biggest items by net value are listed; a line says how many others are left out.
		Negative values are purchase returns, meaning raw material sent back to the supplier. Returns are present only when the data says they are included; if it says returns are not included, say so and give no returns figure.
		Rates are net rates per unit, so they include tax and item discounts. A rate swing is the gap between the lowest and highest recorded rate; describe it as a recorded difference, never as a mistake or overcharging.
		Growth figures compare the second half of the financial year with the first half. They are indicative only while the financial year is still running.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) buys raw material from suppliers and issues it to the kitchen. Purchase value is a cost, not sales. Never call it revenue or sales.
		- The current month is incomplete and months after it have not happened yet, so a small or zero value there is NOT a drop or a fall.
		- Item names can carry extra text such as a size. Keep names exactly as written.
		- Never guess why a purchase or a return was made.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total net purchase value so far this financial year and the number of purchases, then returns (say they are not included if the data says so); " +
		"(2) the top three raw materials by net value, with their value and share of the total; " +
		"(3) the highest and the lowest month by net purchase value among months that have data, leaving out the current incomplete month; " +
		"(4) the supplier with the highest net value and its share (if only one supplier is in the data, say so briefly); " +
		"(5) one thing that needs attention, such as a big rate swing, an item not bought for months or high returns, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top raw materials this year",
		"Which months were busiest?",
		"Which rates changed the most?",
		"Items not bought recently"
	];

	private bool _showAi = false;

	private async Task ToggleAi()
	{
		_showAi = !_showAi;
		if (_showAi)
			_currentDateTime = await CommonData.LoadCurrentDateTime();
	}

	// Text version of what is on screen: _monthlySummaries is the grid, _transactionOverviews is the filtered data behind it.
	// The totals are computed here, because the model is unreliable at adding up hundreds of rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static string Pct(double part, double whole) => whole == 0 ? "n/a" : $"{(part * 100 / whole).ToString("0.#", CultureInfo.InvariantCulture)}%";
		static string OrNone(IEnumerable<string> parts) { var list = parts.ToList(); return list.Count == 0 ? "none" : string.Join("; ", list); }
		static bool IsReturn(PurchaseItemOverviewModel t) => t.Quantity < 0 || t.NetTotal < 0;

		int MonthIndex(DateTime date) => ((date.Year - _fromDate.Year) * 12) + date.Month - _fromDate.Month;

		var rows = _transactionOverviews;
		var purchaseRows = rows.Where(t => !IsReturn(t)).ToList();
		var returnRows = rows.Where(IsReturn).ToList();
		var summaries = _monthlySummaries;
		var months = _monthHeaders.Select(Clean).ToList();
		var currentIndex = MonthIndex(_currentDateTime);
		var grand = summaries.Sum(s => Convert.ToDouble(s.TotalAmount));
		var yearRunning = currentIndex is >= 0 and <= 11;

		var text = new StringBuilder();
		text.AppendLine($"Financial year shown: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		text.AppendLine($"Month columns: {string.Join(", ", months)}");
		text.AppendLine($"Today is {_currentDateTime:dd-MMM-yyyy}. " + (yearRunning
			? $"The current month ({months[currentIndex]}) is incomplete and the months after it have no data yet."
			: currentIndex > 11 ? "This financial year is over." : "This financial year has not started."));
		text.AppendLine($"The month columns show: {(_showAmount ? "net rupee value" : "quantity in each item's own unit")}.");
		text.AppendLine($"Raw material filter: {(_selectedRawMaterial?.Id > 0 ? _selectedRawMaterial.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedRawMaterialCategory?.Id > 0 ? _selectedRawMaterialCategory.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Party filter: {(_selectedParty?.Id > 0 ? _selectedParty.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");
		text.AppendLine(!_showTransactionReturns ? "Purchase returns: NOT included in this data, so there is no returns figure."
			: $"Purchase returns: included; {returnRows.Select(t => t.TransactionNo).Distinct().Count()} returns worth {N(-returnRows.Sum(t => t.NetTotal))} in total.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Purchases: purchases={purchaseRows.Select(t => t.TransactionNo).Distinct().Count()}, net={N(purchaseRows.Sum(t => t.NetTotal))}, " +
			$"discount={N(purchaseRows.Sum(t => t.DiscountAmount))}, tax={N(purchaseRows.Sum(t => t.TotalTaxAmount))}");
		if (_showTransactionReturns)
			text.AppendLine($"Net of returns: net={N(rows.Sum(t => t.NetTotal))}");
		text.AppendLine($"Raw materials in this data: {summaries.Count}");

		text.AppendLine($"Purchase net value by month{(_showTransactionReturns ? " (net of returns)" : string.Empty)}: " + string.Join("; ", Enumerable.Range(0, 12)
			.Select(i => (Month: months[i], Rows: rows.Where(t => MonthIndex(t.TransactionDateTime) == i).ToList()))
			.Select(x => $"{x.Month}={N(x.Rows.Sum(t => t.NetTotal))} ({x.Rows.Where(t => !IsReturn(t)).Select(t => t.TransactionNo).Distinct().Count()} purchases)")));

		text.AppendLine("Top 10 suppliers by net value: " + string.Join("; ", rows
			.GroupBy(t => t.PartyName)
			.Select(g => (Name: g.Key, Value: g.Sum(t => t.NetTotal)))
			.OrderByDescending(x => x.Value).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Value)} (share {Pct(Convert.ToDouble(x.Value), grand)})")));

		var ranked = summaries.OrderByDescending(s => s.TotalAmount).ToList();

		text.AppendLine("Top 15 raw materials by net value: " + string.Join("; ", ranked.Take(15)
			.Select(s => $"{Clean(s.ItemName)}={N(s.TotalAmount)} (share {Pct(Convert.ToDouble(s.TotalAmount), grand)}) / {N(s.TotalQuantity, "0.##")} {Clean(s.UnitOfMeasurement)} / avg rate {N(s.AverageRate, "0.##")} / {s.PartyCount} suppliers")));

		text.AppendLine($"Growth, second half vs first half of the year{(yearRunning ? " (the year is still running, so the second half is incomplete; indicative only)" : string.Empty)}, for items active in at least 3 months:");
		var growth = summaries.Where(s => s.FirstHalf > 0 && s.ActiveMonths >= 3).ToList();
		text.AppendLine("- Growing: " + OrNone(growth.Where(s => s.GrowthPercent > 0).OrderByDescending(s => s.GrowthPercent).Take(5)
			.Select(s => $"{Clean(s.ItemName)}=+{N(s.GrowthPercent, "0.#")}%")));
		text.AppendLine("- Shrinking: " + OrNone(growth.Where(s => s.GrowthPercent < 0).OrderBy(s => s.GrowthPercent).Take(5)
			.Select(s => $"{Clean(s.ItemName)}={N(s.GrowthPercent, "0.#")}%")));

		if (yearRunning)
			text.AppendLine("Items not bought for 2 or more months (months since the last purchase): " + OrNone(summaries
				.Where(s => s.MonthsSinceLastPurchase >= 2).OrderByDescending(s => s.MonthsSinceLastPurchase).Take(10)
				.Select(s => $"{Clean(s.ItemName)}={s.MonthsSinceLastPurchase}")));

		text.AppendLine("Biggest net rate swings (net rate per unit, minimum / maximum / last): " + OrNone(summaries
			.Where(s => s.MaximumRate > s.MinimumRate).OrderByDescending(s => s.RateVariancePercent).Take(8)
			.Select(s => $"{Clean(s.ItemName)}: {N(s.MinimumRate, "0.##")} / {N(s.MaximumRate, "0.##")} / {N(s.LastRate, "0.##")} per {Clean(s.UnitOfMeasurement)} (swing {N(s.RateVariancePercent, "0.#")}%, {s.PartyCount} suppliers)")));

		if (_showTransactionReturns)
			text.AppendLine("Items with the most returned value: " + OrNone(summaries
				.Where(s => s.ReturnAmount > 0).OrderByDescending(s => s.ReturnAmount).Take(10)
				.Select(s => $"{Clean(s.ItemName)}={N(s.ReturnAmount)} / {N(s.ReturnQuantity, "0.##")} {Clean(s.UnitOfMeasurement)} (return {N(s.ReturnPercent, "0.#")}%)")));

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV)");
		text.AppendLine("item,category,uom," + string.Join(',', months) + ",total_in_measure,net_value,avg_rate");

		foreach (var s in ranked.Take(60))
			text.AppendLine(string.Join(',',
				Clean(s.ItemName),
				Clean(s.ItemCategoryName),
				Clean(s.UnitOfMeasurement),
				string.Join(',', Enumerable.Range(0, 12).Select(i => N(s[i], "0.##"))),
				N(s.Total, "0.##"),
				N(s.TotalAmount),
				N(s.AverageRate, "0.##")));

		if (ranked.Count > 60)
			text.AppendLine($"(Other {ranked.Count - 60} raw materials are left out of the table; together their net value is {N(ranked.Skip(60).Sum(s => s.TotalAmount))}.)");

		return text.ToString();
	}
}

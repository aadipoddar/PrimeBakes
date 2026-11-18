using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Summary;

public partial class KitchenIssueItemMonthlyReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the KITCHEN ISSUE ITEM MONTHLY REPORT: raw material issued from the main store to the kitchens, one row per raw material, with one column per month of one financial year. REPORT DATA has the financial year, filters and what the month columns show, then PRECOMPUTED TOTALS and lists, then an ITEM TABLE (CSV).
		The month columns show either quantity (in each item's own unit) or rupee value; the data says which. Raw materials are measured in different units (kg, litres, pieces), so never add up or compare quantities across different items. Rank and compare items by rupee value. Give an item's quantity only together with its own unit.
		ITEM TABLE columns: item, category, uom, one column per month (in what the month columns show), total_in_measure (the 12 months added up), net_value (net issue value in rupees for the year). Only the biggest items by value are listed; a line says how many others are left out.
		Negative values are kitchen issue returns. Returns are present only when the data says they are included; if it says returns are not included, say so and give no returns figure.
		Growth figures compare the second half of the financial year with the first half. They are indicative only while the financial year is still running.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) buys raw material and issues it to the kitchen, where the finished products are made. A kitchen issue is raw material going to a kitchen. A kitchen issue return is raw material coming back from the kitchen, which the business treats as damage. Never guess the cause of a return.
		- Issue value is the cost of raw material, not sales. Never call it revenue or sales.
		- The current month is incomplete and months after it have not happened yet, so a small or zero value there is NOT a drop or a fall.
		- Item names can carry extra text such as a size. Keep names exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total issue value so far this financial year and the number of issues, then returns (say they are not included if the data says so); " +
		"(2) the top three raw materials by value, with their value and share of the total; " +
		"(3) the highest and the lowest month by issue value among months that have data, leaving out the current incomplete month; " +
		"(4) the kitchen with the highest issue value and its share (if only one kitchen is in the data, say so briefly); " +
		"(5) one thing that needs attention, such as an item not issued for months, a big rate swing or high returns, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top raw materials this year",
		"Which months were busiest?",
		"Which items are slowing down?",
		"Items not issued recently"
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
		static bool IsReturn(KitchenIssueItemOverviewModel t) => t.Quantity < 0 || t.Total < 0;

		int MonthIndex(DateTime date) => ((date.Year - _fromDate.Year) * 12) + date.Month - _fromDate.Month;

		var rows = _transactionOverviews;
		var issueRows = rows.Where(t => !IsReturn(t)).ToList();
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
		text.AppendLine($"The month columns show: {(_showAmount ? "rupee value" : "quantity in each item's own unit")}.");
		text.AppendLine($"Raw material filter: {(_selectedRawMaterial?.Id > 0 ? _selectedRawMaterial.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedRawMaterialCategory?.Id > 0 ? _selectedRawMaterialCategory.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Kitchen filter: {(_selectedKitchen?.Id > 0 ? _selectedKitchen.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");
		text.AppendLine(!_showTransactionReturns ? "Kitchen issue returns: NOT included in this data, so there is no returns figure."
			: $"Kitchen issue returns: included; {returnRows.Select(t => t.TransactionNo).Distinct().Count()} returns worth {N(-returnRows.Sum(t => t.Total))} in total.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Issues: issues={issueRows.Select(t => t.TransactionNo).Distinct().Count()}, value={N(issueRows.Sum(t => t.Total))}");
		if (_showTransactionReturns)
			text.AppendLine($"Net of returns: value={N(rows.Sum(t => t.Total))}");
		text.AppendLine($"Raw materials in this data: {summaries.Count}");

		text.AppendLine($"Issue value by month{(_showTransactionReturns ? " (net of returns)" : string.Empty)}: " + string.Join("; ", Enumerable.Range(0, 12)
			.Select(i => (Month: months[i], Rows: rows.Where(t => MonthIndex(t.TransactionDateTime) == i).ToList()))
			.Select(x => $"{x.Month}={N(x.Rows.Sum(t => t.Total))} ({x.Rows.Where(t => !IsReturn(t)).Select(t => t.TransactionNo).Distinct().Count()} issues)")));

		text.AppendLine("Issue value by kitchen, highest first: " + string.Join("; ", rows
			.GroupBy(t => t.KitchenName)
			.Select(g => (Name: g.Key, Value: g.Sum(t => t.Total)))
			.OrderByDescending(x => x.Value)
			.Select(x => $"{Clean(x.Name)}={N(x.Value)} (share {Pct(Convert.ToDouble(x.Value), grand)})")));

		var ranked = summaries.OrderByDescending(s => s.TotalAmount).ToList();

		text.AppendLine("Top 15 raw materials by value: " + string.Join("; ", ranked.Take(15)
			.Select(s => $"{Clean(s.ItemName)}={N(s.TotalAmount)} (share {Pct(Convert.ToDouble(s.TotalAmount), grand)}) / {N(s.TotalQuantity, "0.##")} {Clean(s.UnitOfMeasurement)} / avg rate {N(s.AverageRate, "0.##")}")));

		text.AppendLine($"Growth, second half vs first half of the year{(yearRunning ? " (the year is still running, so the second half is incomplete; indicative only)" : string.Empty)}, for items active in at least 3 months:");
		var growth = summaries.Where(s => s.FirstHalf > 0 && s.ActiveMonths >= 3).ToList();
		text.AppendLine("- Growing: " + OrNone(growth.Where(s => s.GrowthPercent > 0).OrderByDescending(s => s.GrowthPercent).Take(5)
			.Select(s => $"{Clean(s.ItemName)}=+{N(s.GrowthPercent, "0.#")}%")));
		text.AppendLine("- Shrinking: " + OrNone(growth.Where(s => s.GrowthPercent < 0).OrderBy(s => s.GrowthPercent).Take(5)
			.Select(s => $"{Clean(s.ItemName)}={N(s.GrowthPercent, "0.#")}%")));

		if (yearRunning)
			text.AppendLine("Items not issued for 2 or more months (months since the last issue): " + OrNone(summaries
				.Where(s => s.MonthsSinceLastIssue >= 2).OrderByDescending(s => s.MonthsSinceLastIssue).Take(10)
				.Select(s => $"{Clean(s.ItemName)}={s.MonthsSinceLastIssue}")));

		text.AppendLine("Biggest rate swings (issue rate per unit, minimum / maximum / last): " + OrNone(summaries
			.Where(s => s.MaximumRate > s.MinimumRate).OrderByDescending(s => s.RateVariancePercent).Take(5)
			.Select(s => $"{Clean(s.ItemName)}: {N(s.MinimumRate, "0.##")} / {N(s.MaximumRate, "0.##")} / {N(s.LastRate, "0.##")} per {Clean(s.UnitOfMeasurement)} (swing {N(s.RateVariancePercent, "0.#")}%)")));

		if (_showTransactionReturns)
			text.AppendLine("Items with the most returned value: " + OrNone(summaries
				.Where(s => s.ReturnAmount > 0).OrderByDescending(s => s.ReturnAmount).Take(10)
				.Select(s => $"{Clean(s.ItemName)}={N(s.ReturnAmount)} / {N(s.ReturnQuantity, "0.##")} {Clean(s.UnitOfMeasurement)} (return {N(s.ReturnPercent, "0.#")}%)")));

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV)");
		text.AppendLine("item,category,uom," + string.Join(',', months) + ",total_in_measure,net_value");

		foreach (var s in ranked.Take(60))
			text.AppendLine(string.Join(',',
				Clean(s.ItemName),
				Clean(s.ItemCategoryName),
				Clean(s.UnitOfMeasurement),
				string.Join(',', Enumerable.Range(0, 12).Select(i => N(s[i], "0.##"))),
				N(s.Total, "0.##"),
				N(s.TotalAmount)));

		if (ranked.Count > 60)
			text.AppendLine($"(Other {ranked.Count - 60} raw materials are left out of the table; together their net value is {N(ranked.Skip(60).Sum(s => s.TotalAmount))}.)");

		return text.ToString();
	}
}

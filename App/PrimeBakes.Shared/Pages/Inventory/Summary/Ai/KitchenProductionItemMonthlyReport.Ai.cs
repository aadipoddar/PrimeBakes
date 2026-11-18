using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Summary;

public partial class KitchenProductionItemMonthlyReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the KITCHEN PRODUCTION ITEM MONTHLY REPORT: finished products made by the kitchens, one row per product, with one column per month of one financial year. REPORT DATA has the financial year, filters and what the month columns show, then PRECOMPUTED TOTALS and lists, then an ITEM TABLE (CSV).
		The month columns show either units made or rupee value at the rates recorded; the data says which.
		ITEM TABLE columns: item, category, one column per month (in what the month columns show), total_in_measure (the 12 months added up), units (units made for the year), value (rupee value for the year). Only the biggest products by units are listed; a line says how many others are left out.
		Negative values are kitchen production returns. Returns are present only when the data says they are included; if it says returns are not included, say so and give no returns figure.
		Growth figures compare the second half of the financial year with the first half. They are indicative only while the financial year is still running.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) buys raw material, issues it to the kitchen, and the kitchen makes the finished products. A kitchen production is finished products made. A kitchen production return is finished products coming back, which the business treats as damage. Never guess the cause of a return.
		- Production value is what the products are valued at when recorded, not sales. Never call it revenue or sales.
		- Rank products by units unless the question asks for value.
		- The current month is incomplete and months after it have not happened yet, so a small or zero value there is NOT a drop or a fall.
		- Product names can carry a price tag such as "@20". Keep it exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total units made so far this financial year, their value and the number of productions, then returns (say they are not included if the data says so); " +
		"(2) the top three products by units, with their units and share of the total units; " +
		"(3) the highest and the lowest month by units among months that have data, leaving out the current incomplete month; " +
		"(4) the kitchen with the most units and its share (if only one kitchen is in the data, say so briefly); " +
		"(5) one thing that needs attention, such as a product not made for months, a clear shrinking trend or high returns, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Top products this year",
		"Which months were busiest?",
		"Which products are slowing down?",
		"Products not made recently"
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
		static bool IsReturn(KitchenProductionItemOverviewModel t) => t.Quantity < 0 || t.Total < 0;

		int MonthIndex(DateTime date) => ((date.Year - _fromDate.Year) * 12) + date.Month - _fromDate.Month;

		var rows = _transactionOverviews;
		var productionRows = rows.Where(t => !IsReturn(t)).ToList();
		var returnRows = rows.Where(IsReturn).ToList();
		var summaries = _monthlySummaries;
		var months = _monthHeaders.Select(Clean).ToList();
		var currentIndex = MonthIndex(_currentDateTime);
		var grandUnits = summaries.Sum(s => Convert.ToDouble(s.TotalQuantity));
		var yearRunning = currentIndex is >= 0 and <= 11;

		var text = new StringBuilder();
		text.AppendLine($"Financial year shown: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		text.AppendLine($"Month columns: {string.Join(", ", months)}");
		text.AppendLine($"Today is {_currentDateTime:dd-MMM-yyyy}. " + (yearRunning
			? $"The current month ({months[currentIndex]}) is incomplete and the months after it have no data yet."
			: currentIndex > 11 ? "This financial year is over." : "This financial year has not started."));
		text.AppendLine($"The month columns show: {(_showAmount ? "rupee value" : "units made")}.");
		text.AppendLine($"Product filter: {(_selectedProduct?.Id > 0 ? _selectedProduct.Name : "All")}");
		text.AppendLine($"Category filter: {(_selectedProductCategory?.Id > 0 ? _selectedProductCategory.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Kitchen filter: {(_selectedKitchen?.Id > 0 ? _selectedKitchen.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");
		text.AppendLine(!_showTransactionReturns ? "Kitchen production returns: NOT included in this data, so there is no returns figure."
			: $"Kitchen production returns: included; {returnRows.Select(t => t.TransactionNo).Distinct().Count()} returns, {N(-returnRows.Sum(t => t.Quantity), "0.##")} units worth {N(-returnRows.Sum(t => t.Total))} in total.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Productions: productions={productionRows.Select(t => t.TransactionNo).Distinct().Count()}, units={N(productionRows.Sum(t => t.Quantity), "0.##")}, value={N(productionRows.Sum(t => t.Total))}");
		if (_showTransactionReturns)
			text.AppendLine($"Net of returns: units={N(rows.Sum(t => t.Quantity), "0.##")}, value={N(rows.Sum(t => t.Total))}");
		text.AppendLine($"Products in this data: {summaries.Count}");

		text.AppendLine($"Production by month{(_showTransactionReturns ? " (net of returns)" : string.Empty)} (units / value / productions): " + string.Join("; ", Enumerable.Range(0, 12)
			.Select(i => (Month: months[i], Rows: rows.Where(t => MonthIndex(t.TransactionDateTime) == i).ToList()))
			.Select(x => $"{x.Month}={N(x.Rows.Sum(t => t.Quantity), "0.##")} / {N(x.Rows.Sum(t => t.Total))} / {x.Rows.Where(t => !IsReturn(t)).Select(t => t.TransactionNo).Distinct().Count()}")));

		text.AppendLine("Production by kitchen, most units first: " + string.Join("; ", rows
			.GroupBy(t => t.KitchenName)
			.Select(g => (Name: g.Key, Qty: g.Sum(t => t.Quantity), Value: g.Sum(t => t.Total)))
			.OrderByDescending(x => x.Qty)
			.Select(x => $"{Clean(x.Name)}={N(x.Qty, "0.##")} units / {N(x.Value)} (share {Pct(Convert.ToDouble(x.Qty), grandUnits)})")));

		var ranked = summaries.OrderByDescending(s => s.TotalQuantity).ToList();

		text.AppendLine("Top 15 products by units: " + string.Join("; ", ranked.Take(15)
			.Select(s => $"{Clean(s.ItemName)}={N(s.TotalQuantity, "0.##")} units (share {Pct(Convert.ToDouble(s.TotalQuantity), grandUnits)}) / {N(s.TotalAmount)}")));
		text.AppendLine("Slowest 10 products by units (only products with recorded production): " + OrNone(ranked.Where(s => s.TotalQuantity > 0).OrderBy(s => s.TotalQuantity).Take(10)
			.Select(s => $"{Clean(s.ItemName)}={N(s.TotalQuantity, "0.##")}")));
		text.AppendLine("Categories by value: " + string.Join("; ", summaries
			.GroupBy(s => s.ItemCategoryName)
			.Select(g => (Name: g.Key, Qty: g.Sum(s => s.TotalQuantity), Value: g.Sum(s => s.TotalAmount)))
			.OrderByDescending(x => x.Value)
			.Select(x => $"{Clean(x.Name)}={N(x.Value)} / {N(x.Qty, "0.##")} units")));

		text.AppendLine($"Growth, second half vs first half of the year{(yearRunning ? " (the year is still running, so the second half is incomplete; indicative only)" : string.Empty)}, for products active in at least 3 months:");
		var growth = summaries.Where(s => s.FirstHalf > 0 && s.ActiveMonths >= 3).ToList();
		text.AppendLine("- Growing: " + OrNone(growth.Where(s => s.GrowthPercent > 0).OrderByDescending(s => s.GrowthPercent).Take(5)
			.Select(s => $"{Clean(s.ItemName)}=+{N(s.GrowthPercent, "0.#")}%")));
		text.AppendLine("- Shrinking: " + OrNone(growth.Where(s => s.GrowthPercent < 0).OrderBy(s => s.GrowthPercent).Take(5)
			.Select(s => $"{Clean(s.ItemName)}={N(s.GrowthPercent, "0.#")}%")));

		if (yearRunning)
			text.AppendLine("Products not made for 2 or more months (months since the last production): " + OrNone(summaries
				.Where(s => s.MonthsSinceLastProduction >= 2).OrderByDescending(s => s.MonthsSinceLastProduction).Take(10)
				.Select(s => $"{Clean(s.ItemName)}={s.MonthsSinceLastProduction}")));

		if (_showTransactionReturns)
			text.AppendLine("Products with the most returned units: " + OrNone(summaries
				.Where(s => s.ReturnQuantity > 0).OrderByDescending(s => s.ReturnQuantity).Take(10)
				.Select(s => $"{Clean(s.ItemName)}={N(s.ReturnQuantity, "0.##")} units / {N(s.ReturnAmount)} (return {N(s.ReturnPercent, "0.#")}%)")));

		text.AppendLine();
		text.AppendLine("ITEM TABLE (CSV)");
		text.AppendLine("item,category," + string.Join(',', months) + ",total_in_measure,units,value");

		foreach (var s in ranked.Take(60))
			text.AppendLine(string.Join(',',
				Clean(s.ItemName),
				Clean(s.ItemCategoryName),
				string.Join(',', Enumerable.Range(0, 12).Select(i => N(s[i], "0.##"))),
				N(s.Total, "0.##"),
				N(s.TotalQuantity, "0.##"),
				N(s.TotalAmount)));

		if (ranked.Count > 60)
			text.AppendLine($"(Other {ranked.Count - 60} products are left out of the table; together they made {N(ranked.Skip(60).Sum(s => s.TotalQuantity), "0.##")} units worth {N(ranked.Skip(60).Sum(s => s.TotalAmount))}.)");

		return text.ToString();
	}
}

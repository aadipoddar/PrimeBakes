using PrimeBakes.Models.Inventory.Stock;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Stock.Reports;

public partial class RawMaterialStockReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the RAW MATERIAL STOCK REPORT: raw material stock for the period. Each row is a material that had opening stock, movement or closing stock.
		REPORT DATA has the period and category filter, then PRECOMPUTED TOTALS and lists, then a STOCK TABLE (CSV) with the top 100 materials by closing value.
		CSV columns: material, category, unit (unit of measurement), opening, in, out, closing (all in that unit), closing_value, avg_daily_use, days_on_hand, adjustment (in that unit), last_purchase (date).
		closing_value is the closing stock valued at the material's rate. Values in the totals are in rupees and can be added; quantities cannot.
		Raw materials use different units (kg, litre and so on). Never add or compare quantities of different materials. Compare materials by value or by days on hand.
		Days on hand is closing stock divided by the average daily use IN THIS PERIOD. For a period shorter than 7 days it is only a rough guide, and you must say so.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It buys raw material (purchase) and issues it to the kitchen (kitchen issue). Kitchen issue returns are damages.
		- Stock adjustments are manual corrections. A large adjustment means the recorded stock did not match the physical count; never guess why.
		- Negative stock means more was recorded going out than coming in. It is a recording issue to check, not real stock.
		- Purchases are stock bought, not the cost of goods sold.
		- Quote the precomputed lists instead of adding up rows yourself.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the number of materials with stock activity, the total closing value, and the purchase value and kitchen issue value for the period; " +
		"(2) the three materials with the highest closing value, with their closing quantities in their own units and values; " +
		"(3) materials at risk of running out, using the lowest days on hand (say it is a rough guide if the period is under 7 days), or say none are at risk; " +
		"(4) stock that is not moving: materials with closing stock but no consumption, with the largest values; " +
		"(5) one thing that needs attention from this data, such as negative stock or large adjustments, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which materials might run out soon?",
		"Which stock is not moving?",
		"Is any stock negative?",
		"What did we buy and issue the most?"
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

	// Text version of what is on screen (_stockSummary already has the category filter and the "has activity" filter applied).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0.##") => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";
		static string Ymd(IFormattable value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

		string DaysOnHand(RawMaterialStockSummaryModel s) => D(s.AverageDailyConsumption) > 0 ? N(D(s.DaysOnHand), "0.#") + " days" : "no consumption";

		var all = _stockSummary;
		var days = (_toDate.Date - _fromDate.Date).Days + 1;
		var closingValue = D(all.Sum(s => s.ClosingValueByRate));

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy} ({days} days)");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Category filter: {(_selectedCategory?.Id > 0 ? _selectedCategory.Name : "All")}");
		text.AppendLine("Only materials with opening stock, movement or closing stock are in this data.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All materials: materials={all.Count} (quantities are not added across materials because units differ)");
		text.AppendLine($"Values in rupees: opening={N(all.Sum(s => s.OpeningValue), "0")}, in={N(all.Sum(s => s.TotalInValue), "0")}, out={N(all.Sum(s => s.TotalOutValue), "0")}, closing at rate={N(closingValue, "0")}");
		text.AppendLine($"Movement values in rupees: purchase={N(all.Sum(s => s.PurchaseValue), "0")}, purchase return={N(all.Sum(s => s.PurchaseReturnValue), "0")}, kitchen issue={N(all.Sum(s => s.KitchenIssueValue), "0")}, " +
			$"kitchen production={N(all.Sum(s => s.KitchenProductionValue), "0")}, sale={N(all.Sum(s => s.SaleValue), "0")}, sale return={N(all.Sum(s => s.SaleReturnValue), "0")}, " +
			$"stock transfer={N(all.Sum(s => s.StockTransferValue), "0")}, bill={N(all.Sum(s => s.BillValue), "0")}, adjustment={N(all.Sum(s => s.AdjustmentValue), "0")}");

		text.AppendLine("Top 15 materials by closing value: " + string.Join("; ", all
			.OrderByDescending(s => s.ClosingValueByRate).Take(15)
			.Select(s => $"{Clean(s.RawMaterialName)}: {N(s.ClosingStock)} {Clean(s.UnitOfMeasurement)}, value {N(s.ClosingValueByRate, "0")}, days on hand {DaysOnHand(s)}")));

		text.AppendLine("Lowest days on hand (materials with consumption): " + string.Join("; ", all
			.Where(s => D(s.AverageDailyConsumption) > 0 && s.ClosingStock >= 0)
			.OrderBy(s => s.DaysOnHand).Take(10)
			.Select(s => $"{Clean(s.RawMaterialName)}: {N(s.ClosingStock)} {Clean(s.UnitOfMeasurement)}, {N(s.AverageDailyConsumption)} used per day, {DaysOnHand(s)}")));

		text.AppendLine("Stock with no consumption in the period (largest value first): " + string.Join("; ", all
			.Where(s => s.ClosingStock > 0 && D(s.AverageDailyConsumption) <= 0)
			.OrderByDescending(s => s.ClosingValueByRate).Take(10)
			.Select(s => $"{Clean(s.RawMaterialName)}: {N(s.ClosingStock)} {Clean(s.UnitOfMeasurement)}, value {N(s.ClosingValueByRate, "0")}, last purchase {(Ymd(s.LastPurchaseDate).Length == 0 ? "none recorded" : Ymd(s.LastPurchaseDate))}")));

		text.AppendLine("Negative stock: " + string.Join("; ", all
			.Where(s => s.IsNegativeStock || s.ClosingStock < 0)
			.OrderBy(s => s.ClosingStock).Take(15)
			.Select(s => $"{Clean(s.RawMaterialName)}: closing {N(s.ClosingStock)} {Clean(s.UnitOfMeasurement)}")));

		text.AppendLine("Largest stock adjustments by value: " + string.Join("; ", all
			.Where(s => D(s.AdjustmentValue) != 0)
			.OrderByDescending(s => Math.Abs(D(s.AdjustmentValue))).Take(10)
			.Select(s => $"{Clean(s.RawMaterialName)}: {N(s.AdjustmentStock)} {Clean(s.UnitOfMeasurement)} / {N(s.AdjustmentValue, "0")}")));

		text.AppendLine("Largest purchases by value: " + string.Join("; ", all
			.Where(s => D(s.PurchaseValue) > 0)
			.OrderByDescending(s => s.PurchaseValue).Take(10)
			.Select(s => $"{Clean(s.RawMaterialName)}: {N(s.PurchaseStock)} {Clean(s.UnitOfMeasurement)} / {N(s.PurchaseValue, "0")}")));

		text.AppendLine("Largest kitchen issues by value: " + string.Join("; ", all
			.Where(s => D(s.KitchenIssueValue) > 0)
			.OrderByDescending(s => s.KitchenIssueValue).Take(10)
			.Select(s => $"{Clean(s.RawMaterialName)}: {N(s.KitchenIssueStock)} {Clean(s.UnitOfMeasurement)} / {N(s.KitchenIssueValue, "0")}")));

		text.AppendLine("Categories by closing value: " + string.Join("; ", all
			.GroupBy(s => s.RawMaterialCategoryName)
			.Select(g => new { Name = g.Key, Items = g.Count(), Value = D(g.Sum(s => s.ClosingValueByRate)) })
			.OrderByDescending(x => x.Value)
			.Select(x => $"{Clean(x.Name)}={N(x.Value, "0")} ({Pct(x.Value, closingValue)}, {x.Items} materials)")));

		text.AppendLine();
		text.AppendLine("STOCK TABLE (CSV, top 100 materials by closing value)");
		text.AppendLine("material,category,unit,opening,in,out,closing,closing_value,avg_daily_use,days_on_hand,adjustment,last_purchase");

		foreach (var s in all.OrderByDescending(s => s.ClosingValueByRate).Take(100))
			text.AppendLine(string.Join(',',
				Clean(s.RawMaterialName),
				Clean(s.RawMaterialCategoryName),
				Clean(s.UnitOfMeasurement),
				N(s.OpeningStock),
				N(s.InStock),
				N(s.OutStock),
				N(s.ClosingStock),
				N(s.ClosingValueByRate, "0"),
				N(s.AverageDailyConsumption),
				D(s.AverageDailyConsumption) > 0 ? N(D(s.DaysOnHand), "0.#") : string.Empty,
				N(s.AdjustmentStock),
				Ymd(s.LastPurchaseDate)));

		return text.ToString();
	}
}

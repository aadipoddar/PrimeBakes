using PrimeBakes.Models.Inventory.Stock;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Stock.Reports;

public partial class ProductStockReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the PRODUCT STOCK REPORT: finished-product stock at ONE location for the period (the data names the location). Each row is a product that had opening stock, movement or closing stock.
		REPORT DATA has the period, location and category filter, then PRECOMPUTED TOTALS and lists, then a STOCK TABLE (CSV) with the top 100 products by closing value.
		CSV columns: product, category, opening, in, out, closing (all in units), closing_value, avg_daily_use, days_on_hand, adjustment (units), last_sale (date).
		closing_value is the closing stock valued at the product's rate. Values in the totals are in rupees.
		Days on hand is closing stock divided by the average daily use IN THIS PERIOD. For a period shorter than 7 days it is only a rough guide, and you must say so.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It makes finished products in the kitchen (kitchen production) and supplies them to the other outlets, so its stock moves with production and supply. Kitchen production returns are damages.
		- Other locations' stock moves with what they receive and what they sell. Outlet sales reach the software in different ways, so use "recorded" when talking about their movement.
		- Stock adjustments are manual corrections. A large adjustment means the recorded stock did not match the physical count; never guess why.
		- Negative stock means more was recorded going out than coming in. It is a recording issue to check, not real stock.
		- Product names can carry a price tag such as "@20". Keep it exactly as written.
		- Quote the precomputed lists instead of adding up rows yourself.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the location, the number of products with stock activity, total closing units and total closing value; " +
		"(2) the three products with the highest closing value, with their closing units and values; " +
		"(3) products at risk of running out, using the lowest days on hand (say it is a rough guide if the period is under 7 days), or say none are at risk; " +
		"(4) stock that is not moving: products with closing stock but no consumption, with the largest values; " +
		"(5) one thing that needs attention from this data, such as negative stock or large adjustments, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which products might run out soon?",
		"Which stock is not moving?",
		"Is any stock negative?",
		"Where were the biggest adjustments?"
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

		string DaysOnHand(ProductStockSummaryModel s) => D(s.AverageDailyConsumption) > 0 ? N(D(s.DaysOnHand), "0.#") + " days" : "no consumption";

		var all = _stockSummary;
		var days = (_toDate.Date - _fromDate.Date).Days + 1;
		var closingValue = D(all.Sum(s => s.ClosingValueByRate));

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy} ({days} days)");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Location: {(_selectedLocation is null ? "None" : _selectedLocation.Name)}");
		text.AppendLine($"Category filter: {(_selectedCategory?.Id > 0 ? _selectedCategory.Name : "All")}");
		text.AppendLine("Only products with opening stock, movement or closing stock are in this data.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All products: products={all.Count}, opening units={N(all.Sum(s => s.OpeningStock))}, in={N(all.Sum(s => s.InStock))}, out={N(all.Sum(s => s.OutStock))}, closing units={N(all.Sum(s => s.ClosingStock))}");
		text.AppendLine($"Values in rupees: opening={N(all.Sum(s => s.OpeningValue), "0")}, in={N(all.Sum(s => s.TotalInValue), "0")}, out={N(all.Sum(s => s.TotalOutValue), "0")}, closing at rate={N(closingValue, "0")}");
		text.AppendLine($"Movement values in rupees: purchase={N(all.Sum(s => s.PurchaseValue), "0")}, purchase return={N(all.Sum(s => s.PurchaseReturnValue), "0")}, kitchen issue={N(all.Sum(s => s.KitchenIssueValue), "0")}, " +
			$"kitchen production={N(all.Sum(s => s.KitchenProductionValue), "0")}, sale={N(all.Sum(s => s.SaleValue), "0")}, sale return={N(all.Sum(s => s.SaleReturnValue), "0")}, " +
			$"stock transfer={N(all.Sum(s => s.StockTransferValue), "0")}, bill={N(all.Sum(s => s.BillValue), "0")}, adjustment={N(all.Sum(s => s.AdjustmentValue), "0")}");

		text.AppendLine("Top 15 products by closing value: " + string.Join("; ", all
			.OrderByDescending(s => s.ClosingValueByRate).Take(15)
			.Select(s => $"{Clean(s.ProductName)}: {N(s.ClosingStock)} units, value {N(s.ClosingValueByRate, "0")}, days on hand {DaysOnHand(s)}")));

		text.AppendLine("Lowest days on hand (products with consumption): " + string.Join("; ", all
			.Where(s => D(s.AverageDailyConsumption) > 0 && s.ClosingStock >= 0)
			.OrderBy(s => s.DaysOnHand).Take(10)
			.Select(s => $"{Clean(s.ProductName)}: {N(s.ClosingStock)} units, {N(s.AverageDailyConsumption)} used per day, {DaysOnHand(s)}")));

		text.AppendLine("Stock with no consumption in the period (largest value first): " + string.Join("; ", all
			.Where(s => s.ClosingStock > 0 && D(s.AverageDailyConsumption) <= 0)
			.OrderByDescending(s => s.ClosingValueByRate).Take(10)
			.Select(s => $"{Clean(s.ProductName)}: {N(s.ClosingStock)} units, value {N(s.ClosingValueByRate, "0")}, last sale {(Ymd(s.LastSaleDate).Length == 0 ? "none recorded" : Ymd(s.LastSaleDate))}")));

		text.AppendLine("Negative stock: " + string.Join("; ", all
			.Where(s => s.IsNegativeStock || s.ClosingStock < 0)
			.OrderBy(s => s.ClosingStock).Take(15)
			.Select(s => $"{Clean(s.ProductName)}: closing {N(s.ClosingStock)} units")));

		text.AppendLine("Largest stock adjustments by value: " + string.Join("; ", all
			.Where(s => D(s.AdjustmentValue) != 0)
			.OrderByDescending(s => Math.Abs(D(s.AdjustmentValue))).Take(10)
			.Select(s => $"{Clean(s.ProductName)}: {N(s.AdjustmentStock)} units / {N(s.AdjustmentValue, "0")}")));

		text.AppendLine("Most units out: " + string.Join("; ", all
			.OrderByDescending(s => s.OutStock).Take(10)
			.Select(s => $"{Clean(s.ProductName)}={N(s.OutStock)}")));

		text.AppendLine("Categories by closing value: " + string.Join("; ", all
			.GroupBy(s => s.ProductCategoryName)
			.Select(g => new { Name = g.Key, Items = g.Count(), Value = D(g.Sum(s => s.ClosingValueByRate)) })
			.OrderByDescending(x => x.Value)
			.Select(x => $"{Clean(x.Name)}={N(x.Value, "0")} ({Pct(x.Value, closingValue)}, {x.Items} products)")));

		text.AppendLine();
		text.AppendLine("STOCK TABLE (CSV, top 100 products by closing value)");
		text.AppendLine("product,category,opening,in,out,closing,closing_value,avg_daily_use,days_on_hand,adjustment,last_sale");

		foreach (var s in all.OrderByDescending(s => s.ClosingValueByRate).Take(100))
			text.AppendLine(string.Join(',',
				Clean(s.ProductName),
				Clean(s.ProductCategoryName),
				N(s.OpeningStock),
				N(s.InStock),
				N(s.OutStock),
				N(s.ClosingStock),
				N(s.ClosingValueByRate, "0"),
				N(s.AverageDailyConsumption),
				D(s.AverageDailyConsumption) > 0 ? N(D(s.DaysOnHand), "0.#") : string.Empty,
				N(s.AdjustmentStock),
				Ymd(s.LastSaleDate)));

		return text.ToString();
	}
}

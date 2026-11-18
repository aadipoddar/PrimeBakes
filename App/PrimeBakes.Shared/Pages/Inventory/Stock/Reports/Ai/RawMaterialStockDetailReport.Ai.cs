using PrimeBakes.Models.Inventory.Stock;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Stock.Reports;

public partial class RawMaterialStockDetailReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the RAW MATERIAL STOCK DETAIL REPORT: every stock movement line of raw materials for the period.
		Each line has a type (for example purchase, kitchen issue, adjustment), a transaction number, a raw material, a quantity and a total value, as recorded.
		REPORT DATA has the period, then PRECOMPUTED TOTALS and lists, then a MOVEMENT TABLE (CSV) with the 150 largest material and type combinations by absolute total value.
		CSV columns: material, type, lines (movement lines), quantity (sum as recorded, in the material's own unit), total (sum as recorded, rupees).
		Raw materials use different units (kg, litre and so on). Never add or compare quantities of different materials. Totals in rupees can be added.
		Quantity and total are exactly as recorded. Do not assume a sign convention; look at the signs in the data before saying whether a type adds or removes stock.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It buys raw material (purchase) and issues it to the kitchen (kitchen issue). Kitchen issue returns are damages.
		- A stock adjustment is a manual correction. A large adjustment means the recorded stock did not match the physical count; never guess why.
		- Purchases are stock bought, not the cost of goods sold.
		- Quote the precomputed lists instead of adding up rows yourself.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the number of movement lines, different materials and transactions for the period; " +
		"(2) the movement types with the most lines and their total values; " +
		"(3) the materials with the most movement lines; " +
		"(4) the stock adjustments in the period: how many, their total value, and the largest one; say there are none if there are none; " +
		"(5) one thing that needs attention from this data, such as large adjustments, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Summarize movements by type",
		"Which materials move the most?",
		"What adjustments were made?",
		"Which days were busiest?"
	];

	// ---------- AI wiring ----------
	private bool _showAi = false;

	private void ToggleAi() => _showAi = !_showAi;

	// Text version of what is on screen (_stockDetails).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0.##") => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static bool IsAdjustment(RawMaterialStockDetailsModel t) => string.Equals(t.Type, "adjustment", StringComparison.OrdinalIgnoreCase);

		var all = _stockDetails;
		var days = (_toDate.Date - _fromDate.Date).Days + 1;

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy} ({days} days)");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All lines: lines={all.Count}, different materials={all.Select(t => t.RawMaterialName).Distinct().Count()}, " +
			$"transactions={all.Select(t => t.TransactionNo).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Count()}, total value={N(all.Sum(t => t.Total), "0")}");

		text.AppendLine("By type, most lines first: " + string.Join("; ", all
			.GroupBy(t => t.Type ?? "unknown")
			.Select(g => new { Name = g.Key, Lines = g.Count(), Materials = g.Select(t => t.RawMaterialName).Distinct().Count(), Total = g.Sum(t => t.Total) })
			.OrderByDescending(x => x.Lines)
			.Select(x => $"{Clean(x.Name)}={x.Lines} lines / {x.Materials} materials / total {N(x.Total, "0")}")));

		text.AppendLine("Top 15 materials by number of movement lines: " + string.Join("; ", all
			.GroupBy(t => t.RawMaterialName)
			.Select(g => new { Name = g.Key, Lines = g.Count(), Total = g.Sum(t => t.Total) })
			.OrderByDescending(x => x.Lines).Take(15)
			.Select(x => $"{Clean(x.Name)}={x.Lines} lines / total {N(x.Total, "0")}")));

		text.AppendLine("Top 10 materials by absolute total value: " + string.Join("; ", all
			.GroupBy(t => t.RawMaterialName)
			.Select(g => new { Name = g.Key, Total = D(g.Sum(t => t.Total)) })
			.OrderByDescending(x => Math.Abs(x.Total)).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Total, "0")}")));

		var adjustments = all.Where(IsAdjustment).ToList();
		text.AppendLine($"Stock adjustments: lines={adjustments.Count}, total value={N(adjustments.Sum(t => t.Total), "0")}");
		text.AppendLine("Largest adjustments by absolute value: " + string.Join("; ", adjustments
			.OrderByDescending(t => Math.Abs(D(t.Total))).Take(15)
			.Select(t => $"{Clean(t.TransactionNo)} on {t.TransactionDateTime:dd-MMM-yyyy}: {Clean(t.RawMaterialName)}, quantity {N(t.Quantity)}, value {N(t.Total, "0")}")));

		if (days > 1)
			text.AppendLine("Busiest days by number of lines: " + string.Join("; ", all
				.GroupBy(t => t.TransactionDateTime.Date)
				.Select(g => new { Day = g.Key, Lines = g.Count() })
				.OrderByDescending(x => x.Lines).Take(5)
				.Select(x => $"{x.Day:dd-MMM-yyyy}={x.Lines} lines")));

		text.AppendLine();
		text.AppendLine("MOVEMENT TABLE (CSV, 150 largest material and type combinations by absolute total value)");
		text.AppendLine("material,type,lines,quantity,total");

		var rows = all
			.GroupBy(t => (Material: t.RawMaterialName, Type: t.Type ?? "unknown"))
			.Select(g => new { g.Key.Material, g.Key.Type, Lines = g.Count(), Quantity = g.Sum(t => t.Quantity), Total = D(g.Sum(t => t.Total)) })
			.OrderByDescending(x => Math.Abs(x.Total)).Take(150);

		foreach (var x in rows)
			text.AppendLine(string.Join(',', Clean(x.Material), Clean(x.Type), x.Lines.ToString(), N(x.Quantity), N(x.Total, "0")));

		return text.ToString();
	}
}

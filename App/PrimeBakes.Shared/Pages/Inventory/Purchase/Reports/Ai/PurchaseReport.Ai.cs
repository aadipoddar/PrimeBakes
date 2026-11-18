using PrimeBakes.Models.Inventory.Purchase;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Inventory.Purchase.Reports;

public partial class PurchaseReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the PURCHASE REPORT: raw material bought from suppliers (the "party" on each purchase). REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and lists, then a time series line, then a PARTY TABLE (CSV).
		PARTY TABLE columns: party (supplier), purchases (number of purchases, can be blank), returns (number of purchase returns, can be blank), total (final amount in rupees, tax included; net of returns when returns are included), discount (item discounts plus cash discount).
		Quantities are deliberately not given: raw materials are measured in different units (kg, litres, pieces), so quantities cannot be added up. Talk about rupee value only. "item lines" means the number of raw material lines on the purchases, not a quantity.
		Negative amounts are purchase returns, meaning raw material sent back to the supplier. Returns are present only when the data says they are included; if it says returns are not included, say so and give no returns figure.
		In the whole-period summary view there is one row per supplier and no purchase counts, dates or largest-purchase list. If asked for those, say that view does not have them.

		HOW THE BUSINESS WORKS
		- The main outlet (PRIME BAKES OFFICE) buys raw material from suppliers and issues it to the kitchen. Purchase value is a cost, not sales. Never call it revenue or sales.
		- Discounts are terms agreed with the supplier, so do not present them as a concern.
		- A supplier with no purchases in the period just has "no recorded purchases".
		- Never guess why a purchase or a return was made.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) number of purchases and their total value (if the data is the summary view, leave out the number and say it is not available); " +
		"(2) returns: number and value (say they are not included if the data says so); " +
		"(3) the supplier with the highest value and its share of the total; if only one supplier is in the data, say so briefly; " +
		"(4) the busiest day by value and anything unusual in the daily pattern; if the period is a single day or the summary view, say that briefly; " +
		"(5) one thing that needs attention, such as one supplier making up most of the value, high returns or a sharp rise in recent days, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which supplier did we buy the most from?",
		"How much were the returns?",
		"What are the busiest days?",
		"Show purchases by supplier"
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
	// The totals are computed here, because the model is unreliable at adding up hundreds of rows.
	// The table has one row per supplier, so its size depends on the supplier count, not the period.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static bool IsReturn(PurchaseOverviewModel t) => t.TotalAmount < 0 || t.TotalQuantity < 0;
		static decimal Discount(PurchaseOverviewModel t) => t.ItemDiscountAmount + t.CashDiscountAmount;

		var all = _transactionOverviews;

		// The summary view is already one row per supplier with returns netted in, so purchases and returns cannot be told apart there
		var purchaseRows = _showSummary ? all : all.Where(t => !IsReturn(t)).ToList();
		var returnRows = _showSummary ? new List<PurchaseOverviewModel>() : all.Where(IsReturn).ToList();

		var days = (_toDate.Date - _fromDate.Date).Days + 1;
		var (grain, format) = days switch
		{
			<= 2 => ("Hourly", "yyyy-MM-dd HH:00"),
			<= 92 => ("Daily", "yyyy-MM-dd"),
			_ => ("Monthly", "yyyy-MM")
		};

		var grand = Convert.ToDouble(all.Sum(t => t.TotalAmount));
		string Share(double part) => grand == 0 ? "n/a" : $"{(part * 100 / grand).ToString("0.#", CultureInfo.InvariantCulture)}%";

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Party filter: {(_selectedParty?.Id > 0 ? _selectedParty.Name : "All")}");
		text.AppendLine($"Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		text.AppendLine(!_showTransactionReturns ? "Purchase returns: NOT included in this data, so there is no returns figure."
			: _showSummary ? "Purchase returns: included, netted into the totals."
			: $"Purchase returns: included as negative rows; {returnRows.Count} returns worth {N(-returnRows.Sum(t => t.TotalAmount))} in total.");

		if (_showSummary)
			text.AppendLine("This is the SUMMARY view: one row per supplier for the whole period, so there is no purchase count and no split by day.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Purchases: {(_showSummary ? string.Empty : $"purchases={purchaseRows.Count}, ")}item lines={N(purchaseRows.Sum(t => t.TotalItems))}, " +
			$"total={N(purchaseRows.Sum(t => t.TotalAmount))}, discount={N(purchaseRows.Sum(Discount))}");
		if (_showTransactionReturns && !_showSummary)
			text.AppendLine($"Net of returns: total={N(all.Sum(t => t.TotalAmount))}, discount={N(all.Sum(Discount))}");

		var parties = all
			.GroupBy(t => t.PartyName)
			.Select(g => (Name: g.Key, Rows: g.ToList()))
			.OrderByDescending(x => x.Rows.Sum(t => t.TotalAmount))
			.ToList();

		text.AppendLine($"Suppliers: {parties.Count} in this data");
		text.AppendLine($"Top 15 suppliers by total{(_showTransactionReturns ? " (net of returns)" : string.Empty)}: " + string.Join("; ", parties.Take(15)
			.Select(x => $"{Clean(x.Name)}={N(x.Rows.Sum(t => t.TotalAmount))} (share {Share(Convert.ToDouble(x.Rows.Sum(t => t.TotalAmount)))})")));

		if (!_showSummary)
		{
			text.AppendLine("Largest 5 purchases: " + string.Join("; ", purchaseRows
				.OrderByDescending(t => t.TotalAmount).Take(5)
				.Select(t => $"{Clean(t.TransactionNo)}={N(t.TotalAmount)} ({Clean(t.PartyName)}, {t.TransactionDateTime:dd-MMM-yyyy})")));

			if (days > 1)
				text.AppendLine("Top 5 days by total: " + string.Join("; ", all
					.GroupBy(t => t.TransactionDateTime.Date)
					.Select(g => (Day: g.Key, Total: g.Sum(t => t.TotalAmount)))
					.OrderByDescending(x => x.Total).Take(5)
					.Select(x => $"{x.Day:dd-MMM-yyyy}={N(x.Total)}")));

			text.AppendLine($"{grain} total: " + string.Join("; ", all
				.GroupBy(t => t.TransactionDateTime.ToString(format))
				.OrderBy(g => g.Key)
				.Select(g => $"{g.Key}={N(g.Sum(t => t.TotalAmount))}")));
		}

		text.AppendLine();
		text.AppendLine("PARTY TABLE (CSV, whole period)");
		text.AppendLine("party,purchases,returns,total,discount");

		foreach (var x in parties)
			text.AppendLine(string.Join(',',
				Clean(x.Name),
				_showSummary ? string.Empty : x.Rows.Count(t => !IsReturn(t)).ToString(),   // summary rows are already totals, so a count would mislead
				_showSummary ? string.Empty : x.Rows.Count(IsReturn).ToString(),
				N(x.Rows.Sum(t => t.TotalAmount)),
				N(x.Rows.Sum(Discount))));

		return text.ToString();
	}
}

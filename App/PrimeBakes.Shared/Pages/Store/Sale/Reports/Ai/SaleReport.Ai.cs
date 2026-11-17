using PrimeBakes.Models.Store.Sale;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Sale.Reports;

public partial class SaleReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the SALE REPORT. REPORT DATA has the period and filters, then PRECOMPUTED TOTALS, then a "Granularity" line and a CSV table.
		CSV columns: period, type, location, bills (can be blank), qty, total (final amount), discount, cash, upi, card, credit.
		type is "supply" for the main outlet's internal supply to other outlets and "outlet" for every other location.
		Granularity says whether each row is one hour, one day, one month or the whole period for a location.
		Negative amounts are sale returns. Returns, stock transfers and restaurant bills are present only when the data says they are included; if it says returns are not included, say so and give no returns figure.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It supplies products to the other outlets on credit. That is internal supply, not customer sales. So its credit sales and its lack of cash, card or UPI are normal, and credit figures are not customers' unpaid bills. Its discounts are part of the supply terms to outlets, so do not present them as a concern.
		- For any question about customer sales, busiest days, comparing locations or the payment mode split, use the outlet figures only and say that supply is left out. Mention supply separately and briefly, only if it helps.
		- The other outlets are Coco (company owned and managed), Fofo (franchise owned) or Supply (a different kind of outlet). They sell to customers for cash, UPI or card.
		- Outlet sales reach the software in three ways: some outlets bill live, some have the whole day's sales entered at the end of the day, and for some only the remaining amount is handed over to the main outlet. So an outlet with low or no recorded sales is NOT necessarily quiet or weak. Never call an outlet inactive or underperforming; say "recorded sales" instead.
		- Returns are either damaged stock sent back to the main outlet or customers returning items. The data does not say which, so call them "returns" and never assume a cause.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) outlet sales total and number of bills, then the main outlet's supply total, then returns (say they are not included if the data says so); " +
		"(2) the outlet with the highest total and the one with the lowest total; if only one outlet is in the data, say so briefly; " +
		"(3) the cash, UPI, card and credit split of outlet sales as percentages; " +
		"(4) the busiest day by outlet sales and anything unusual in the daily pattern; " +
		"(5) one thing that needs attention, such as high returns or a sharp fall in recent days against earlier days, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which location sold the most?",
		"How much were discounts and returns?",
		"Show the payment mode split",
		"What are the busiest days?"
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
	// The totals are computed here, because the model is unreliable at adding up hundreds of rows.
	// The CSV is aggregated more coarsely the longer the period, so a long period stays small.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ');
		static bool IsSupply(SaleOverviewModel t) => t.LocationId == MainOutletLocationId;

		string Totals(IEnumerable<SaleOverviewModel> rows) =>
			$"{(_showSummary ? string.Empty : $"bills={rows.Count()}, ")}qty={N(rows.Sum(t => t.TotalQuantity), "0.##")}, total={N(rows.Sum(t => t.TotalAmount))}, " +
			$"discount={N(rows.Sum(t => t.DiscountAmount))}, cash={N(rows.Sum(t => t.Cash))}, upi={N(rows.Sum(t => t.UPI))}, " +
			$"card={N(rows.Sum(t => t.Card))}, credit={N(rows.Sum(t => t.Credit))}";

		var all = _transactionOverviews;
		var outlets = all.Where(t => !IsSupply(t)).ToList();
		var supply = all.Where(IsSupply).ToList();

		var days = (_toDate.Date - _fromDate.Date).Days + 1;
		var (grain, format) = days switch
		{
			<= 2 => ("hour", "yyyy-MM-dd HH:00"),
			<= 92 => ("day", "yyyy-MM-dd"),
			_ => ("month", "yyyy-MM")
		};

		// The summary view has no dates and is already one row per location
		if (_showSummary) grain = "whole period";

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Location filter: {(_selectedLocation?.Id > 0 ? _selectedLocation.Name : "All")}");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Party filter: {(_selectedParty?.Id > 0 ? _selectedParty.Name : "All")}");
		text.AppendLine($"Online partner filter: {(_selectedOnlinePartner?.Id > 0 ? _selectedOnlinePartner.Name : "All")}");
		text.AppendLine($"Stock transfers: {(_showStockTransfers ? "included" : "not included")}. Restaurant bills: {(_showBills ? "included" : "not included")}. Deleted transactions: {(_showDeleted ? "included" : "not included")}.");

		var returnRows = all.Where(t => t.TotalAmount < 0).ToList();
		text.AppendLine(!_showSaleReturns ? "Sale returns: NOT included in this data, so there is no returns figure."
			: _showSummary ? "Sale returns: included, netted into the totals."
			: $"Sale returns: included as negative rows; {returnRows.Count} returns worth {N(-returnRows.Sum(t => t.TotalAmount))} in total.");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Outlet sales (all outlets except the main outlet): {Totals(outlets)}");
		text.AppendLine($"Main outlet internal supply (PRIME BAKES OFFICE): {Totals(supply)}");
		text.AppendLine($"All rows together: {Totals(all)}");
		text.AppendLine("Outlet total by location, highest first: " + string.Join("; ", outlets
			.GroupBy(t => t.LocationName)
			.Select(g => (Name: g.Key, Total: g.Sum(t => t.TotalAmount)))
			.OrderByDescending(x => x.Total)
			.Select(x => $"{Clean(x.Name)}={N(x.Total)}")));

		if (!_showSummary && days > 1)
			text.AppendLine("Top 5 days by outlet sales: " + string.Join("; ", outlets
				.GroupBy(t => t.TransactionDateTime.Date)
				.Select(g => (Day: g.Key, Total: g.Sum(t => t.TotalAmount)))
				.OrderByDescending(x => x.Total).Take(5)
				.Select(x => $"{x.Day:dd-MMM-yyyy}={N(x.Total)}")));

		text.AppendLine();
		text.AppendLine($"Granularity: one row per {grain} per location");
		text.AppendLine("period,type,location,bills,qty,total,discount,cash,upi,card,credit");

		var rows = all
			.GroupBy(t => (Period: _showSummary ? "all" : t.TransactionDateTime.ToString(format), Location: t.LocationName, Supply: IsSupply(t)))
			.OrderBy(g => g.Key.Period).ThenBy(g => g.Key.Location);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				g.Key.Period,
				g.Key.Supply ? "supply" : "outlet",
				Clean(g.Key.Location),
				_showSummary ? string.Empty : g.Count().ToString(),   // summary rows are already totals, so a count would mislead
				N(g.Sum(t => t.TotalQuantity), "0.##"),
				N(g.Sum(t => t.TotalAmount)),
				N(g.Sum(t => t.DiscountAmount)),
				N(g.Sum(t => t.Cash)),
				N(g.Sum(t => t.UPI)),
				N(g.Sum(t => t.Card)),
				N(g.Sum(t => t.Credit))));

		return text.ToString();
	}
}

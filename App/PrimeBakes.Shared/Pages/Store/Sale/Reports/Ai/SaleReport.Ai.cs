using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Store.Sale.Reports;

public partial class SaleReport
{
	private bool _showAi = false;
	private DateTime _aiToday = DateTime.Now.Date;

	private async Task ToggleAi()
	{
		_showAi = !_showAi;
		if (_showAi)
			_aiToday = (await CommonData.LoadCurrentDateTime()).Date;
	}

	// Text version of what is on screen (_transactionOverviews already has every filter applied).
	// CSV keeps a long period small; the period is aggregated more coarsely the longer it is.
	private string BuildAiContext()
	{
		static string N(IFormattable value) => value.ToString("0.##", CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ');

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
		text.AppendLine($"Includes: sale returns {(_showSaleReturns ? "yes" : "no")}, stock transfers {(_showStockTransfers ? "yes" : "no")}, restaurant bills {(_showBills ? "yes" : "no")}, deleted transactions {(_showDeleted ? "yes" : "no")}");
		text.AppendLine($"Granularity: one row per {grain} per location");
		text.AppendLine("period,location,bills,qty,total,discount,cash,upi,card,credit");

		var rows = _transactionOverviews
			.GroupBy(t => (Period: _showSummary ? "all" : t.TransactionDateTime.ToString(format), Location: t.LocationName))
			.OrderBy(g => g.Key.Period).ThenBy(g => g.Key.Location);

		foreach (var g in rows)
			text.AppendLine(string.Join(',',
				g.Key.Period,
				Clean(g.Key.Location),
				_showSummary ? string.Empty : g.Count().ToString(),   // summary rows are already totals, so a count would mislead
				N(g.Sum(t => t.TotalQuantity)),
				N(g.Sum(t => t.TotalAmount)),
				N(g.Sum(t => t.DiscountAmount)),
				N(g.Sum(t => t.Cash)),
				N(g.Sum(t => t.UPI)),
				N(g.Sum(t => t.Card)),
				N(g.Sum(t => t.Credit))));

		return text.ToString();
	}
}

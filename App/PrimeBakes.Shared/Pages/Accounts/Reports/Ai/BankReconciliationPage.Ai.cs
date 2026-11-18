using PrimeBakes.Models.Accounts.FinancialAccounting;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Accounts.Reports;

public partial class BankReconciliationPage
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the BANK RECONCILIATION page: ledger lines of the selected ledgers (normally bank ledgers) in the period, and whether each has cleared the bank. All amounts are in rupees.
		A line is "reconciled" when it has a clearing date and "pending" when it has none.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and lists, then a PENDING LINES table (CSV) with the 150 oldest pending lines.
		CSV columns: date, ledger, voucher, transaction_no, instrument_no (cheque or reference number), instrument_date, debit, credit, days_pending.
		For a bank ledger, a debit is money received (a deposit) and a credit is money paid (a payment). Pending debits are receipts not yet cleared; pending credits are payments not yet cleared.
		An instrument dated after today is post-dated; say so rather than calling it late.
		Pending only means the line has no clearing date. The data does not say why, so never assume a cause.
		This report gives figures only. Do not give accounting, tax or audit advice.

		HOW THE BUSINESS WORKS
		- Lines in the data are as on screen. Reconciled lines show how long clearing took; pending lines show how long they have waited.
		- Copy ledger names and transaction numbers exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the number of lines, how many are reconciled and pending, and the pending debit and credit totals; " +
		"(2) the bank ledger with the largest pending amounts, with its pending debits and credits; " +
		"(3) the oldest pending lines, with how many days they have been pending; " +
		"(4) how long reconciled lines took to clear on average; " +
		"(5) one thing that needs attention from this data, such as long-pending lines, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"What is still pending?",
		"Which lines are oldest?",
		"How long do lines take to clear?",
		"Are there post-dated instruments?"
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

	// Text version of what is on screen (_transactionOverviews already has every filter applied).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ').Replace('\n', ' ').Replace('\r', ' ');
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Pct(decimal part, decimal whole) => whole == 0 ? "0%" : $"{N(part / whole * 100, "0.#")}%";

		// Dates are handled as yyyy-MM-dd text so this works whether a model date is DateTime, nullable or DateOnly
		static string Ymd(IFormattable value) => value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
		static DateTime? Day(IFormattable value) =>
			DateTime.TryParseExact(Ymd(value), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;

		var all = _transactionOverviews;
		var pending = all.Where(l => l.ClearingDate is null).ToList();
		var reconciled = all.Where(l => l.ClearingDate is not null).ToList();
		var today = _aiToday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

		int Age(FinancialAccountingLedgerOverviewModel l) => (_aiToday - l.TransactionDateTime.Date).Days;

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}.");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Account type filter: {(_selectedAccountType?.Id > 0 ? _selectedAccountType.Name : "All")}");
		text.AppendLine($"Ledger filter: {(_selectedLedger?.Id > 0 ? _selectedLedger.Name : "All")}");
		text.AppendLine($"Lines shown: {(_reconciledFilter == YesNoFilterOptions.Yes ? "reconciled only" : _reconciledFilter == YesNoFilterOptions.No ? "pending only" : "reconciled and pending")}.");

		var pendingDebit = D(pending.Sum(l => l.Debit));
		var pendingCredit = D(pending.Sum(l => l.Credit));

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All lines: lines={all.Count}, debit={N(all.Sum(l => l.Debit))}, credit={N(all.Sum(l => l.Credit))}");
		text.AppendLine($"Reconciled: lines={reconciled.Count} ({Pct(reconciled.Count, all.Count)}), debit={N(reconciled.Sum(l => l.Debit))}, credit={N(reconciled.Sum(l => l.Credit))}");
		text.AppendLine($"Pending: lines={pending.Count} ({Pct(pending.Count, all.Count)}), debit (receipts)={N(pendingDebit)}, credit (payments)={N(pendingCredit)}, net pending debit less credit={N(pendingDebit - pendingCredit)}");

		text.AppendLine("Pending by age (days since the transaction date): " + string.Join("; ", new[]
		{
			("0 to 7 days", 0, 7), ("8 to 30 days", 8, 30), ("31 to 90 days", 31, 90), ("over 90 days", 91, int.MaxValue)
		}.Select(b =>
		{
			var rows = pending.Where(l => Age(l) >= b.Item2 && Age(l) <= b.Item3).ToList();
			return $"{b.Item1}: {rows.Count} lines, debit {N(rows.Sum(l => l.Debit))}, credit {N(rows.Sum(l => l.Credit))}";
		})));

		text.AppendLine("Pending by ledger, largest first: " + string.Join("; ", pending
			.GroupBy(l => l.LedgerName)
			.Select(g => new { Name = g.Key, Lines = g.Count(), Debit = D(g.Sum(l => l.Debit)), Credit = D(g.Sum(l => l.Credit)) })
			.OrderByDescending(x => x.Debit + x.Credit).Take(10)
			.Select(x => $"{Clean(x.Name)}: {x.Lines} lines, debit {N(x.Debit)}, credit {N(x.Credit)}")));

		var postDated = pending
			.Where(l => Ymd(l.InstrumentDate).Length > 0 && string.CompareOrdinal(Ymd(l.InstrumentDate), today) > 0)
			.ToList();
		text.AppendLine($"Pending lines with an instrument date after today (post-dated): {postDated.Count}, debit {N(postDated.Sum(l => l.Debit))}, credit {N(postDated.Sum(l => l.Credit))}");

		var lead = reconciled
			.Select(l => new { Ledger = l.LedgerName, Cleared = Day(l.ClearingDate), l.TransactionDateTime })
			.Where(x => x.Cleared is not null)
			.Select(x => new { x.Ledger, Days = (x.Cleared!.Value - x.TransactionDateTime.Date).Days })
			.ToList();

		text.AppendLine(lead.Count == 0
			? "Time to clear: no reconciled lines in the data."
			: $"Time to clear (transaction date to clearing date), all reconciled lines: average {N(lead.Average(x => x.Days), "0.#")} days over {lead.Count} lines");
		text.AppendLine("Average time to clear by ledger: " + string.Join("; ", lead
			.GroupBy(x => x.Ledger)
			.Select(g => new { Name = g.Key, Lines = g.Count(), Average = g.Average(x => x.Days) })
			.OrderByDescending(x => x.Average).Take(10)
			.Select(x => $"{Clean(x.Name)}={N(x.Average, "0.#")} days ({x.Lines} lines)")));

		text.AppendLine();
		text.AppendLine("PENDING LINES (CSV, 150 oldest pending lines)");
		text.AppendLine("date,ledger,voucher,transaction_no,instrument_no,instrument_date,debit,credit,days_pending");

		foreach (var l in pending.OrderBy(l => l.TransactionDateTime).Take(150))
			text.AppendLine(string.Join(',',
				$"{l.TransactionDateTime:yyyy-MM-dd}", Clean(l.LedgerName), Clean(l.VoucherName), Clean(l.TransactionNo),
				Clean(l.InstrumentNo), Ymd(l.InstrumentDate), N(l.Debit), N(l.Credit), Age(l).ToString()));

		return text.ToString();
	}
}

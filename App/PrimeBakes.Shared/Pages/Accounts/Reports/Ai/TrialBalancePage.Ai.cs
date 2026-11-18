using PrimeBakes.Models.Accounts.FinancialAccounting;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Accounts.Reports;

public partial class TrialBalancePage
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the TRIAL BALANCE: one row per ledger with its opening balance, the debits and credits of the period, and its closing balance. All amounts are in rupees.
		REPORT DATA has the period and filters, then PRECOMPUTED TOTALS and lists, then a LEDGER TABLE (CSV) with up to 300 ledgers, largest closing balance first.
		CSV columns: ledger, group, type (account type), nature (Assets, Liabilities, Income or Expenses), open_dr, open_cr, debit, credit, close_dr, close_cr.
		In a trial balance, total debits should equal total credits. The data gives both totals and their difference; report the difference exactly as given.
		Net balance of a ledger is closing debit less closing credit, shown as Dr or Cr. Assets and expenses normally carry a debit balance; liabilities and income normally carry a credit balance.
		A ledger on the opposite side is listed as "unusual side". It is worth checking, but it is not necessarily an error. Never guess why.
		This report gives figures only. Do not give accounting, tax or audit advice.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It supplies the other outlets on credit, so large balances on outlet ledgers are supply on credit, not bad debts and not overdue.
		- Copy ledger, group and account type names exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the number of ledgers, total closing debits, total closing credits, and whether they match (give the difference); " +
		"(2) the net closing balance by nature (Assets, Liabilities, Income, Expenses), each with Dr or Cr; " +
		"(3) the three ledgers with the largest net closing balances, with their side; " +
		"(4) the groups with the largest net closing balances, top three; " +
		"(5) one thing that needs attention from this data, such as ledgers on an unusual side or a difference between debits and credits, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Does the trial balance match?",
		"Which ledgers are on an unusual side?",
		"What moved the most in this period?",
		"Show balances by group"
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

	// Text version of what is on screen (_trialBalance already has the group and account type filters applied).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Side(decimal net) => net >= 0 ? $"{N(net)} Dr" : $"{N(-net)} Cr";
		static decimal Net(TrialBalanceModel t) => D(t.ClosingDebit) - D(t.ClosingCredit);
		static bool Unusual(TrialBalanceModel t)
		{
			var net = Net(t);
			return t.NatureName switch
			{
				"Assets" or "Expenses" => net < -0.5m,
				"Liabilities" or "Income" => net > 0.5m,
				_ => false
			};
		}

		var all = _trialBalance;

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy} (closing balances are as at {_toDate:dd-MMM-yyyy})");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");
		text.AppendLine($"Group filter: {(_selectedGroup?.Id > 0 ? _selectedGroup.Name : "All")}");
		text.AppendLine($"Account type filter: {(_selectedAccountType?.Id > 0 ? _selectedAccountType.Name : "All")}");

		var openDr = D(all.Sum(t => t.OpeningDebit));
		var openCr = D(all.Sum(t => t.OpeningCredit));
		var debit = D(all.Sum(t => t.Debit));
		var credit = D(all.Sum(t => t.Credit));
		var closeDr = D(all.Sum(t => t.ClosingDebit));
		var closeCr = D(all.Sum(t => t.ClosingCredit));

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"All ledgers shown: ledgers={all.Count}");
		text.AppendLine($"Opening: debit={N(openDr)}, credit={N(openCr)}, difference={N(openDr - openCr)}");
		text.AppendLine($"Period movement: debit={N(debit)}, credit={N(credit)}, difference={N(debit - credit)}");
		text.AppendLine($"Closing: debit={N(closeDr)}, credit={N(closeCr)}, difference={N(closeDr - closeCr)}");

		text.AppendLine("Net closing balance by nature: " + string.Join("; ", all
			.GroupBy(t => t.NatureName ?? "No nature")
			.Select(g => new { Name = g.Key, Count = g.Count(), Net = g.Sum(t => Net(t)) })
			.OrderByDescending(x => Math.Abs(x.Net))
			.Select(x => $"{Clean(x.Name)}={Side(x.Net)} ({x.Count} ledgers)")));

		text.AppendLine("Net closing balance by group, largest first: " + string.Join("; ", all
			.GroupBy(t => t.GroupName ?? "No group")
			.Select(g => new { Name = g.Key, Count = g.Count(), Net = g.Sum(t => Net(t)) })
			.OrderByDescending(x => Math.Abs(x.Net)).Take(15)
			.Select(x => $"{Clean(x.Name)}={Side(x.Net)} ({x.Count} ledgers)")));

		text.AppendLine("Net closing balance by account type, largest first: " + string.Join("; ", all
			.GroupBy(t => t.AccountTypeName ?? "No type")
			.Select(g => new { Name = g.Key, Net = g.Sum(t => Net(t)) })
			.OrderByDescending(x => Math.Abs(x.Net)).Take(10)
			.Select(x => $"{Clean(x.Name)}={Side(x.Net)}")));

		text.AppendLine("Top 15 ledgers by net closing balance: " + string.Join("; ", all
			.OrderByDescending(t => Math.Abs(Net(t))).Take(15)
			.Select(t => $"{Clean(t.LedgerName)} ({Clean(t.NatureName)}): {Side(Net(t))}")));

		text.AppendLine("Ledgers with the largest movement in the period: " + string.Join("; ", all
			.OrderByDescending(t => D(t.Debit) + D(t.Credit)).Take(10)
			.Select(t => $"{Clean(t.LedgerName)}: debit {N(t.Debit)}, credit {N(t.Credit)}")));

		var unusual = all.Where(Unusual).OrderByDescending(t => Math.Abs(Net(t))).ToList();
		text.AppendLine($"Ledgers on an unusual side: {unusual.Count}");
		text.AppendLine("Largest unusual-side ledgers: " + string.Join("; ", unusual.Take(10)
			.Select(t => $"{Clean(t.LedgerName)} ({Clean(t.NatureName)}): {Side(Net(t))}")));

		text.AppendLine();
		text.AppendLine("LEDGER TABLE (CSV, up to 300 ledgers by size of net closing balance)");
		text.AppendLine("ledger,group,type,nature,open_dr,open_cr,debit,credit,close_dr,close_cr");

		foreach (var t in all.OrderByDescending(t => Math.Abs(Net(t))).Take(300))
			text.AppendLine(string.Join(',',
				Clean(t.LedgerName), Clean(t.GroupName), Clean(t.AccountTypeName), Clean(t.NatureName),
				N(t.OpeningDebit), N(t.OpeningCredit), N(t.Debit), N(t.Credit), N(t.ClosingDebit), N(t.ClosingCredit)));

		return text.ToString();
	}
}

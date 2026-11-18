using PrimeBakes.Models.Accounts.FinancialAccounting;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Accounts.Reports;

public partial class BalanceSheetPage
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the BALANCE SHEET: the ledgers whose nature is Assets and the ledgers whose nature is Liabilities, as at the end of the period. All amounts are in rupees.
		REPORT DATA has the period and company filter, then PRECOMPUTED TOTALS and lists, then a LEDGER TABLE (CSV).
		CSV columns: ledger, side (asset or liability), group, opening, closing, debit, credit.
		For an asset, opening and closing are debit balance less credit balance. For a liability they are credit balance less debit balance. So a positive number is the normal side and a negative number is the opposite side.
		This page lists only the Assets and Liabilities natures. So total assets and total liabilities need not be equal. Report the difference exactly as given and never guess why.
		A ledger on the opposite side is listed as "unusual side". It is worth checking, but it is not necessarily an error. Never guess why.
		This report gives figures only. Do not give accounting, tax or audit advice, and do not work out ratios unless asked.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It supplies the other outlets on credit, so large balances on outlet ledgers are supply on credit, not bad debts and not overdue.
		- Copy ledger and group names exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total assets and total liabilities as at the end of the period, and the difference between them; " +
		"(2) how total assets and total liabilities changed from the opening to the closing of the period, in rupees and percentages; " +
		"(3) the three largest asset ledgers with their closing balances; " +
		"(4) the three largest liability ledgers with their closing balances; " +
		"(5) one thing that needs attention from this data, such as ledgers on an unusual side, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Summarize assets and liabilities",
		"What are the largest assets?",
		"What are the largest liabilities?",
		"Which ledgers are on an unusual side?"
	];

	// ---------- AI wiring ----------
	private bool _showAi = false;

	private void ToggleAi() => _showAi = !_showAi;

	// Text version of what is on screen (_assetsTrialBalance and _liabilitiesTrialBalance).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Pct(decimal part, decimal whole) => whole == 0 ? "no base" : $"{N(part / whole * 100, "0.#")}%";
		static decimal AssetClose(TrialBalanceModel t) => D(t.ClosingDebit) - D(t.ClosingCredit);
		static decimal AssetOpen(TrialBalanceModel t) => D(t.OpeningDebit) - D(t.OpeningCredit);
		static decimal LiabClose(TrialBalanceModel t) => D(t.ClosingCredit) - D(t.ClosingDebit);
		static decimal LiabOpen(TrialBalanceModel t) => D(t.OpeningCredit) - D(t.OpeningDebit);

		var assets = _assetsTrialBalance;
		var liabilities = _liabilitiesTrialBalance;

		var assetsClose = assets.Sum(t => AssetClose(t));
		var assetsOpen = assets.Sum(t => AssetOpen(t));
		var liabClose = liabilities.Sum(t => LiabClose(t));
		var liabOpen = liabilities.Sum(t => LiabOpen(t));

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy} (closing balances are as at {_toDate:dd-MMM-yyyy})");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Assets: ledgers={assets.Count}, opening={N(assetsOpen)}, closing={N(assetsClose)}, change={N(assetsClose - assetsOpen)} ({Pct(assetsClose - assetsOpen, Math.Abs(assetsOpen))})");
		text.AppendLine($"Liabilities: ledgers={liabilities.Count}, opening={N(liabOpen)}, closing={N(liabClose)}, change={N(liabClose - liabOpen)} ({Pct(liabClose - liabOpen, Math.Abs(liabOpen))})");
		text.AppendLine($"Closing assets less closing liabilities: {N(assetsClose - liabClose)}");

		text.AppendLine("Assets by group, largest first: " + string.Join("; ", assets
			.GroupBy(t => t.GroupName ?? "No group")
			.Select(g => new { Name = g.Key, Count = g.Count(), Close = g.Sum(t => AssetClose(t)) })
			.OrderByDescending(x => Math.Abs(x.Close)).Take(12)
			.Select(x => $"{Clean(x.Name)}={N(x.Close)} ({x.Count} ledgers, {Pct(x.Close, assetsClose)} of assets)")));

		text.AppendLine("Liabilities by group, largest first: " + string.Join("; ", liabilities
			.GroupBy(t => t.GroupName ?? "No group")
			.Select(g => new { Name = g.Key, Count = g.Count(), Close = g.Sum(t => LiabClose(t)) })
			.OrderByDescending(x => Math.Abs(x.Close)).Take(12)
			.Select(x => $"{Clean(x.Name)}={N(x.Close)} ({x.Count} ledgers, {Pct(x.Close, liabClose)} of liabilities)")));

		text.AppendLine("Top 15 asset ledgers by closing balance: " + string.Join("; ", assets
			.OrderByDescending(t => Math.Abs(AssetClose(t))).Take(15)
			.Select(t => $"{Clean(t.LedgerName)} ({Clean(t.GroupName)}): {N(AssetClose(t))}")));

		text.AppendLine("Top 15 liability ledgers by closing balance: " + string.Join("; ", liabilities
			.OrderByDescending(t => Math.Abs(LiabClose(t))).Take(15)
			.Select(t => $"{Clean(t.LedgerName)} ({Clean(t.GroupName)}): {N(LiabClose(t))}")));

		var unusualAssets = assets.Where(t => AssetClose(t) < -0.5m).OrderBy(t => AssetClose(t)).ToList();
		var unusualLiabilities = liabilities.Where(t => LiabClose(t) < -0.5m).OrderBy(t => LiabClose(t)).ToList();
		text.AppendLine($"Assets with a credit balance (unusual side): {unusualAssets.Count}. " + string.Join("; ", unusualAssets.Take(10)
			.Select(t => $"{Clean(t.LedgerName)}: {N(AssetClose(t))}")));
		text.AppendLine($"Liabilities with a debit balance (unusual side): {unusualLiabilities.Count}. " + string.Join("; ", unusualLiabilities.Take(10)
			.Select(t => $"{Clean(t.LedgerName)}: {N(LiabClose(t))}")));

		text.AppendLine();
		text.AppendLine("LEDGER TABLE (CSV)");
		text.AppendLine("ledger,side,group,opening,closing,debit,credit");

		foreach (var t in assets.OrderByDescending(t => Math.Abs(AssetClose(t))).Take(200))
			text.AppendLine(string.Join(',', Clean(t.LedgerName), "asset", Clean(t.GroupName), N(AssetOpen(t)), N(AssetClose(t)), N(t.Debit), N(t.Credit)));

		foreach (var t in liabilities.OrderByDescending(t => Math.Abs(LiabClose(t))).Take(200))
			text.AppendLine(string.Join(',', Clean(t.LedgerName), "liability", Clean(t.GroupName), N(LiabOpen(t)), N(LiabClose(t)), N(t.Debit), N(t.Credit)));

		return text.ToString();
	}
}

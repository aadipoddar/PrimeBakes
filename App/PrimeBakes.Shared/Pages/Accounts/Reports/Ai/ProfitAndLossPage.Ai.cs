using PrimeBakes.Models.Accounts.FinancialAccounting;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Accounts.Reports;

public partial class ProfitAndLossPage
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the PROFIT AND LOSS report: the ledgers whose nature is Income and the ledgers whose nature is Expenses. All amounts are in rupees.
		REPORT DATA has the period and company filter, then PRECOMPUTED TOTALS and lists, then a LEDGER TABLE (CSV).
		CSV columns: ledger, type (income or expense), group, period_amount, closing_amount.
		period_amount is what was booked within the selected dates: for income it is credits less debits, for an expense it is debits less credits. closing_amount is the balance at the end of the period on the same basis; it includes anything carried in from before the period.
		Quote the period basis unless the question asks for balances. Both bases are given in the totals.
		"Profit" here means income less expenses as recorded in these ledgers, nothing more. Do not call it gross profit, net profit or operating profit, and do not add stock or tax adjustments.
		This report gives figures only. Do not give accounting, tax or audit advice.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It supplies the other outlets on credit, so supply income from outlets is internal supply, not customer sales.
		- A short period, or a period that ends today, can be incomplete. Never call a partial period a drop or a loss of momentum because it is smaller than a full one.
		- Copy ledger and group names exactly as written.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total income, total expenses and profit (income less expenses) for the period, and the profit as a percentage of income; " +
		"(2) the three largest income ledgers with their amounts and shares of income; " +
		"(3) the three largest expense ledgers with their amounts and shares of income; " +
		"(4) the expense groups with the largest amounts, top three; " +
		"(5) one thing that needs attention from this data, such as an expense that is large against income or ledgers on an unusual side, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Did we make a profit?",
		"What are the biggest expenses?",
		"Where does income come from?",
		"Which expenses are large against income?"
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

	// Text version of what is on screen (_incomeTrialBalance and _expenseTrialBalance).
	// Totals and rankings are computed here, because the model is unreliable at adding up rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ').Replace(';', ' ');
		static decimal D(IConvertible value) => Convert.ToDecimal(value, CultureInfo.InvariantCulture);
		static string Pct(decimal part, decimal whole) => whole == 0 ? "no base" : $"{N(part / whole * 100, "0.#")}%";
		static decimal IncomePeriod(TrialBalanceModel t) => D(t.Credit) - D(t.Debit);
		static decimal IncomeClose(TrialBalanceModel t) => D(t.ClosingCredit) - D(t.ClosingDebit);
		static decimal ExpensePeriod(TrialBalanceModel t) => D(t.Debit) - D(t.Credit);
		static decimal ExpenseClose(TrialBalanceModel t) => D(t.ClosingDebit) - D(t.ClosingCredit);

		var income = _incomeTrialBalance;
		var expense = _expenseTrialBalance;

		var incomePeriod = income.Sum(t => IncomePeriod(t));
		var expensePeriod = expense.Sum(t => ExpensePeriod(t));
		var incomeClose = income.Sum(t => IncomeClose(t));
		var expenseClose = expense.Sum(t => ExpenseClose(t));

		var text = new StringBuilder();
		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		if (_toDate.Date >= _aiToday)
			text.AppendLine($"Today is {_aiToday:dd-MMM-yyyy}; the last day of the period is incomplete.");
		text.AppendLine($"Company filter: {(_selectedCompany?.Id > 0 ? _selectedCompany.Name : "All")}");

		text.AppendLine();
		text.AppendLine("PRECOMPUTED TOTALS (exact; use these instead of adding up rows yourself)");
		text.AppendLine($"Period basis: income={N(incomePeriod)}, expenses={N(expensePeriod)}, profit (income less expenses)={N(incomePeriod - expensePeriod)} ({Pct(incomePeriod - expensePeriod, incomePeriod)} of income), expenses are {Pct(expensePeriod, incomePeriod)} of income");
		text.AppendLine($"Closing balance basis (includes anything carried in before the period): income={N(incomeClose)}, expenses={N(expenseClose)}, profit={N(incomeClose - expenseClose)}");
		text.AppendLine($"Ledgers: income ledgers={income.Count}, expense ledgers={expense.Count}");

		text.AppendLine("Income by group, largest first: " + string.Join("; ", income
			.GroupBy(t => t.GroupName ?? "No group")
			.Select(g => new { Name = g.Key, Count = g.Count(), Amount = g.Sum(t => IncomePeriod(t)) })
			.OrderByDescending(x => Math.Abs(x.Amount)).Take(12)
			.Select(x => $"{Clean(x.Name)}={N(x.Amount)} ({Pct(x.Amount, incomePeriod)} of income, {x.Count} ledgers)")));

		text.AppendLine("Expenses by group, largest first: " + string.Join("; ", expense
			.GroupBy(t => t.GroupName ?? "No group")
			.Select(g => new { Name = g.Key, Count = g.Count(), Amount = g.Sum(t => ExpensePeriod(t)) })
			.OrderByDescending(x => Math.Abs(x.Amount)).Take(12)
			.Select(x => $"{Clean(x.Name)}={N(x.Amount)} ({Pct(x.Amount, incomePeriod)} of income, {x.Count} ledgers)")));

		text.AppendLine("Top 15 income ledgers (period): " + string.Join("; ", income
			.OrderByDescending(t => Math.Abs(IncomePeriod(t))).Take(15)
			.Select(t => $"{Clean(t.LedgerName)} ({Clean(t.GroupName)}): {N(IncomePeriod(t))} ({Pct(IncomePeriod(t), incomePeriod)})")));

		text.AppendLine("Top 15 expense ledgers (period): " + string.Join("; ", expense
			.OrderByDescending(t => Math.Abs(ExpensePeriod(t))).Take(15)
			.Select(t => $"{Clean(t.LedgerName)} ({Clean(t.GroupName)}): {N(ExpensePeriod(t))} ({Pct(ExpensePeriod(t), incomePeriod)} of income)")));

		var unusualIncome = income.Where(t => IncomePeriod(t) < -0.5m).OrderBy(t => IncomePeriod(t)).ToList();
		var unusualExpense = expense.Where(t => ExpensePeriod(t) < -0.5m).OrderBy(t => ExpensePeriod(t)).ToList();
		text.AppendLine($"Income ledgers with a net debit in the period (unusual side): {unusualIncome.Count}. " + string.Join("; ", unusualIncome.Take(10)
			.Select(t => $"{Clean(t.LedgerName)}: {N(IncomePeriod(t))}")));
		text.AppendLine($"Expense ledgers with a net credit in the period (unusual side): {unusualExpense.Count}. " + string.Join("; ", unusualExpense.Take(10)
			.Select(t => $"{Clean(t.LedgerName)}: {N(ExpensePeriod(t))}")));

		text.AppendLine();
		text.AppendLine("LEDGER TABLE (CSV)");
		text.AppendLine("ledger,type,group,period_amount,closing_amount");

		foreach (var t in income.OrderByDescending(t => Math.Abs(IncomePeriod(t))).Take(150))
			text.AppendLine(string.Join(',', Clean(t.LedgerName), "income", Clean(t.GroupName), N(IncomePeriod(t)), N(IncomeClose(t))));

		foreach (var t in expense.OrderByDescending(t => Math.Abs(ExpensePeriod(t))).Take(150))
			text.AppendLine(string.Join(',', Clean(t.LedgerName), "expense", Clean(t.GroupName), N(ExpensePeriod(t)), N(ExpenseClose(t))));

		return text.ToString();
	}
}

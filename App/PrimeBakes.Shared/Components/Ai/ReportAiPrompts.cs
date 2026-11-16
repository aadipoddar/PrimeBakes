namespace PrimeBakes.Shared.Components.Ai;

/// <summary>One set of instructions, default question and quick questions per report page.</summary>
public static class ReportAiPrompts
{
	private const string Base = """
		You are the business analyst for Prime Bakes, a bakery company. All money is in Indian rupees (₹).

		RULES
		- Use only the data below. Never invent numbers, splits or causes. If the data cannot answer the question, say what is missing.
		- Copy location, product and party names exactly as written.
		- Quote the numbers. Write amounts with Indian digit grouping, for example ₹14,60,750, and write percentages with the % sign.
		- The latest day or month in a period can be incomplete. Never call a partial period a drop or a fall because it is smaller than a full one.

		STYLE
		- Plain text only: no markdown, no asterisks, no headings. Use "-" for bullets.
		- Short and direct. If the question is specific, answer it first in one or two sentences, then add at most 3 supporting bullets. If the question asks for a fixed list, give exactly that list.
		""";

	public static string Build(string pageInstructions, string data, string question) => $"""
		{Base}

		{pageInstructions}

		REPORT DATA
		{data}

		QUESTION
		{question}
		""";

	// ---------- Sale Report ----------

	public const string SaleReport = """
		ABOUT THIS REPORT
		This is the SALE REPORT. REPORT DATA starts with the period and the active filters, then a "Granularity" line, then a CSV table.
		CSV columns: period, location, bills (can be blank), qty, total (final amount), discount, cash, upi, card, credit.
		Granularity says whether each row is one hour, one day, one month or the whole period for a location.
		Negative amounts are sale returns. Returns, stock transfers and restaurant bills are in the table only when the filters say they are included.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet (head office). It sells products to the other outlets on credit. That is internal supply, so its credit sales and its lack of cash, card or UPI are normal. Do not present them as a concern, and do not read credit figures as customers' unpaid bills.
		- The other outlets are Coco (company owned and managed), Fofo (franchise owned) or Supply (a different kind of outlet). They sell to customers for cash, UPI or card.
		- Outlet sales reach the software in three ways: some outlets bill live, some have the whole day's sales entered at the end of the day, and for some only the remaining amount is handed over to the main outlet. So an outlet with low or no recorded sales is NOT necessarily quiet or weak. Never call an outlet inactive or underperforming; say "recorded sales" instead.
		- Returns are either damaged stock sent back to the main outlet or customers returning items. The data does not say which, so call them "returns" and never assume a cause.
		""";

	public const string SaleReportDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) total sales, number of bills and returns for the period; " +
		"(2) the location with the highest total and the one with the lowest total, leaving out PRIME BAKES OFFICE; if only one location is in the data, say so briefly; " +
		"(3) the cash, UPI, card and credit split as percentages of the total; " +
		"(4) the busiest hour, day or month (whichever the rows are) and any unusual spike or dip; " +
		"(5) one thing that needs attention, such as high returns or heavy discounts, or say that nothing needs attention.";

	public static readonly IReadOnlyList<string> SaleReportSuggestions =
	[
		"Which location sold the most?",
		"How much were discounts and returns?",
		"Show the payment mode split",
		"What are the busiest days?"
	];
}

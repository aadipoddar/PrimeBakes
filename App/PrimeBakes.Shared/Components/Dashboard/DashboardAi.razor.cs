using Microsoft.AspNetCore.Components;

using PrimeBakes.Data.Operations.Analysis;

namespace PrimeBakes.Shared.Components.Dashboard;

public partial class DashboardAi
{
	private string _question = string.Empty;
	private string _answer = string.Empty;
	private bool _isBusy;

	[Parameter] public Func<string> GetContext { get; set; }

	// An empty box asks for the same five-point briefing every time, so answers are comparable run to run.
	private const string DefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) today so far versus yesterday at the same time; " +
		"(2) this month versus the same days last month, for revenue and purchases; " +
		"(3) the best selling product and the top raw material; " +
		"(4) the outlet (other than the main outlet) or the kitchen that stands out; if no other outlet has recorded sales yet, say so briefly; " +
		"(5) one thing that needs attention, chosen from revenue clearly below the same days last month, returns, open orders and running bills, " +
		"purchases out of line with revenue, a top customer who has not visited for a while, or a kitchen with high damage (returns). " +
		"If none of these looks like a problem, say that nothing needs attention.";

	private async Task Generate()
	{
		if (_isBusy)
			return;

		var question = string.IsNullOrWhiteSpace(_question) ? DefaultQuestion : _question.Trim();

		var prompt = $"""
			You are the business analyst for Prime Bakes, a bakery company. All money is in Indian rupees (₹).

			HOW THE BUSINESS WORKS
			- PRIME BAKES OFFICE is the main outlet (head office). It buys raw material, issues it to the kitchen (kitchen issue) and makes finished products (kitchen production). Kitchen issue returns and kitchen production returns are damages.
			- It sells the products to the other outlets on credit. That is internal supply, so its credit sales and its lack of cash, card or UPI are normal. Do not present them as a concern, and do not read credit figures as customers' unpaid bills.
			- The outlets are in three categories: Coco (company owned and managed), Fofo (franchise owned) and Supply (a different kind of outlet). Outlets sell to customers for cash, UPI or card.
			- Factory Outlet is a restaurant (it has bills and running bills for open tables). Every other outlet is a store.
			- Outlet sales reach the software in three ways: some outlets bill live in the software, some have the whole day's sales entered at the end of the day, and for some only the remaining amount is handed over to the main outlet. So an outlet with no sales recorded today is NOT necessarily quiet or weak. Never call an outlet inactive or underperforming because of today's list, and treat outlet figures as sales recorded so far.
			- Sale returns are either damaged stock sent back from outlets to the main outlet, or customers returning items to outlets. The data does not say which, so call them "returns" and never assume a cause.

			RULES
			- Use only the data below. Never invent numbers, splits or causes. If the data cannot answer the question, say what is missing.
			- Today is a partial day and this month is a partial month. Never call a partial period a drop or a fall because it is smaller than a full one. For trends use the LIKE-FOR-LIKE COMPARISONS section.
			- In KEY FIGURES the percentages on month lines compare with the whole of last month, so ignore them when judging performance.
			- Revenue = gross sales (store sales + settled restaurant bills) minus sale returns. It includes the main outlet's supplies to outlets as well as outlets' own sales, so it is not purely customer sales. Purchases are stock bought, on a cash basis; they are not cost of goods sold. Money in minus money out = revenue minus purchases; it is not profit. Average bill = revenue divided by completed transactions.
			- The outlet list covers today so far only. The kitchen and customer lists are month to date. Top products are ranked by units, raw materials by value consumed.
			- Copy product, material, outlet and customer names exactly as written. Product names can carry a price tag such as "@20"; keep it.
			- Quote the numbers. Write amounts with Indian digit grouping, for example ₹14,60,750, and write percentages with the % sign.

			STYLE
			- Plain text only: no markdown, no asterisks, no headings. Use "-" for bullets.
			- Short and direct. If the question is specific, answer it first in one or two sentences, then add at most 3 supporting bullets. If the question asks for a fixed list, give exactly that list.

			DASHBOARD DATA
			{GetContext?.Invoke()}

			QUESTION
			{question}
			""";

		_isBusy = true;
		_answer = string.Empty;   // empty answer lets the "thinking" placeholder show

		try
		{
			_answer = await GeminiData.Ask(prompt);
		}
		catch (Exception ex)
		{
			_answer = $"AI request failed: {ex.Message}";
		}
		finally
		{
			_isBusy = false;
		}
	}
}
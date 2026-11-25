using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Sale;
using PrimeBakes.Models.Store.StockTransfer;

using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class FallacyReport
{
	// ---------- AI prompt for this page ----------
	private const string AiInstructions = """
		ABOUT THIS REPORT
		This is the FALLACY REPORT. It lists transactions that look inconsistent, grouped by check. REPORT DATA has the period and the limits in use, then PRECOMPUTED COUNTS per check and transaction type, then a CSV of the flagged transactions.
		CSV columns: check, type, transaction_no, date, location, party, total, discount_percent, round_off, credit.
		The CSV holds at most 30 rows per check and type (largest amounts first), so use the counts for "how many" and the CSV only for examples.

		THE CHECKS
		- Discount, main outlet to outlet (sale, sale return, stock transfer): the discount is not equal to the receiving outlet's fixed discount.
		- Discount, outlet to customer (sale, sale return, bill): the discount is above the maximum customer discount in the data.
		- Posting (sale, sale return, stock transfer, bill): a transaction with an amount, from an earlier day, has no accounting entry.
		- Round off (sale, sale return, stock transfer, bill): the round off, up or down, is above the maximum round off in the data.
		- Duplicate (sale, sale return, stock transfer, bill): same location, party, total and minute as another transaction. These are possible duplicates, not confirmed ones.
		- Odd timing (sale, sale return, stock transfer, bill): the transaction was entered at an unusual hour, between the start and end hour in the data.
		- Credit (outlet sale, bill): credit on a sale or bill at an outlet or the restaurant.

		HOW THE BUSINESS WORKS
		- PRIME BAKES OFFICE is the main outlet. It supplies the other outlets on credit, so credit there is normal and is not flagged by the Credit check.
		- Outlets and the restaurant sell to customers for cash, UPI or card, so credit on their sales and bills is unusual.
		- A flagged transaction is something to look at, not proof of a mistake. Never accuse anyone; say "needs checking".
		""";

	private const string AiDefaultQuestion =
		"Give exactly five one-line bullets, in this order: " +
		"(1) the total number of flagged transactions and which checks have none; " +
		"(2) the check with the most flagged transactions; " +
		"(3) the location with the most flagged transactions; " +
		"(4) the largest flagged amount with its transaction number; " +
		"(5) what to look at first, or say that nothing needs attention.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which check has the most problems?",
		"Which location has the most flagged transactions?",
		"What should I fix first?",
		"Show the biggest discount conflicts"
	];

	private const int AiMaxRowsPerList = 30;

	private bool _showAi = false;

	private void ToggleAi() => _showAi = !_showAi;

	// Text version of what is on the tabs. The counts are computed here, because the model is unreliable at counting rows.
	private string BuildAiContext()
	{
		static string N(IFormattable value, string format = "0") => value.ToString(format, CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace(',', ' ');

		var text = new StringBuilder();
		var counts = new StringBuilder();
		var csv = new StringBuilder();

		text.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		text.AppendLine($"Today is {_currentDateTime:dd-MMM-yyyy}. The posting check ignores today's transactions.");
		text.AppendLine($"Maximum customer discount: {_customerMaxDiscountThreshold}%. Maximum round off: {N(_maxRoundOffAmount, "0.##")}. Odd timing hours: {_oddTimingStartHour}:00 to {_oddTimingEndHour}:00.");

		// One call per tab: adds a count line and the CSV rows (capped) for that tab
		void Add<T>(string check, string type, List<T> rows,
			Func<T, string> transactionNo, Func<T, DateTime> date, Func<T, string> location, Func<T, string> party,
			Func<T, decimal> total, Func<T, decimal> discount, Func<T, decimal> roundOff, Func<T, decimal> credit)
		{
			counts.AppendLine($"{check} / {type}: {rows.Count} flagged, total {N(rows.Sum(total))}");

			foreach (var t in rows.OrderByDescending(r => Math.Abs(total(r))).Take(AiMaxRowsPerList))
				csv.AppendLine(string.Join(',',
					check, type, Clean(transactionNo(t)), date(t).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
					Clean(location(t)), Clean(party(t)),
					N(total(t)), N(discount(t), "0.##"), N(roundOff(t), "0.##"), N(credit(t), "0.##")));
		}

		void AddSale(string check, string type, List<SaleOverviewModel> rows) =>
			Add(check, type, rows, t => t.TransactionNo, t => t.TransactionDateTime, t => t.LocationName, t => t.PartyName,
				t => t.TotalAmount, t => t.DiscountPercent, t => t.RoundOffAmount, t => t.Credit);

		void AddSaleReturn(string check, string type, List<SaleReturnOverviewModel> rows) =>
			Add(check, type, rows, t => t.TransactionNo, t => t.TransactionDateTime, t => t.LocationName, t => t.PartyName,
				t => t.TotalAmount, t => t.DiscountPercent, t => t.RoundOffAmount, t => t.Credit);

		void AddStockTransfer(string check, string type, List<StockTransferOverviewModel> rows) =>
			Add(check, type, rows, t => t.TransactionNo, t => t.TransactionDateTime, t => t.LocationName, t => t.ToLocationName,
				t => t.TotalAmount, t => t.DiscountPercent, t => t.RoundOffAmount, t => t.Credit);

		void AddBill(string check, string type, List<BillOverviewModel> rows) =>
			Add(check, type, rows, t => t.TransactionNo, t => t.TransactionDateTime, t => t.LocationName, t => string.Empty,
				t => t.TotalAmount, t => t.DiscountPercent, t => t.RoundOffAmount, t => t.Credit);

		AddSale("discount main outlet", "sale", _fallacyDiscountMainOutletSaleOverviews);
		AddSaleReturn("discount main outlet", "sale return", _fallacyDiscountMainOutletSaleReturnOverviews);
		AddStockTransfer("discount main outlet", "stock transfer", _fallacyDiscountMainOutletStockTransferOverviews);
		AddSale("discount outlet customer", "sale", _fallacyDiscountOutletCustomerSaleOverviews);
		AddSaleReturn("discount outlet customer", "sale return", _fallacyDiscountOutletCustomerSaleReturnOverviews);
		AddBill("discount outlet customer", "bill", _fallacyDiscountCustomerBillOverviews);

		AddSale("posting", "sale", _fallacyMissingPostingSaleOverviews);
		AddSaleReturn("posting", "sale return", _fallacyMissingPostingSaleReturnOverviews);
		AddStockTransfer("posting", "stock transfer", _fallacyMissingPostingStockTransferOverviews);
		AddBill("posting", "bill", _fallacyMissingPostingBillOverviews);

		AddSale("round off", "sale", _fallacyRoundOffSaleOverviews);
		AddSaleReturn("round off", "sale return", _fallacyRoundOffSaleReturnOverviews);
		AddStockTransfer("round off", "stock transfer", _fallacyRoundOffStockTransferOverviews);
		AddBill("round off", "bill", _fallacyRoundOffBillOverviews);

		AddSale("duplicate", "sale", _fallacyDuplicateSaleOverviews);
		AddSaleReturn("duplicate", "sale return", _fallacyDuplicateSaleReturnOverviews);
		AddStockTransfer("duplicate", "stock transfer", _fallacyDuplicateStockTransferOverviews);
		AddBill("duplicate", "bill", _fallacyDuplicateBillOverviews);

		AddSale("odd timing", "sale", _fallacyOddTimingSaleOverviews);
		AddSaleReturn("odd timing", "sale return", _fallacyOddTimingSaleReturnOverviews);
		AddStockTransfer("odd timing", "stock transfer", _fallacyOddTimingStockTransferOverviews);
		AddBill("odd timing", "bill", _fallacyOddTimingBillOverviews);

		AddSale("credit", "outlet sale", _fallacyCreditOutletCustomerSaleOverviews);
		AddBill("credit", "bill", _fallacyCreditCustomerBillOverviews);

		text.AppendLine();
		text.AppendLine("PRECOMPUTED COUNTS (exact; use these instead of counting rows yourself)");
		text.Append(counts);
		text.AppendLine();
		text.AppendLine("check,type,transaction_no,date,location,party,total,discount_percent,round_off,credit");
		text.Append(csv);

		return text.ToString();
	}
}

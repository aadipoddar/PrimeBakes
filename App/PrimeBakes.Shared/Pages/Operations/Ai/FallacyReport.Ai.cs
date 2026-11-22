using System.Globalization;
using System.Text;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class FallacyReport
{
	private const string AiInstructions = """
		ABOUT THIS REPORT
		The Fallacy Report is a review queue for automated transaction checks. A flagged row means it matched the rule described here; it is not proof of an error or misconduct.
		- Main outlet discount checks compare a sale, sale return or transfer discount with the destination outlet's configured discount.
		- Outlet customer sale, sale return and bill checks flag discount percentages above the configured customer maximum.
		- Credit checks flag credit amounts on outlet customer sales and bills.
		- Missing posting checks flag non-zero transactions from an earlier day that have no linked accounting transaction number. Today's entries are excluded because posting may still be in progress.
		- Round-off checks flag active transactions when the absolute round-off amount is greater than the configured limit. Positive and negative amounts are both checked.
		- Possible duplicate checks group transactions at the same location, with the same total, and within the same minute (and the same party where applicable). They are candidates to compare, not confirmed duplicates.

		HOW TO INTERPRET THE DATA
		- Treat all flags as items to review; legitimate explanations may exist. Do not accuse users or infer fraud, intent, or causes.
		- Use the supplied counts and sample transaction references. Samples are capped, so do not describe them as the complete list.
		- Do not combine counts across categories as if they were unique transactions; one transaction can match more than one check.
		- Use the exact thresholds and period in the context. The latest day can be incomplete.
		""";

	private const string AiDefaultQuestion =
		"Give exactly five short bullets: (1) flagged-row counts for the checks, (2) discount alerts compared with the configured limits, (3) missing accounting postings, (4) round-off and possible duplicate alerts, and (5) items to review using the supplied transaction references. State clearly that alerts are candidates for review, not confirmed errors. Do not add counts across categories.";

	private static readonly IReadOnlyList<string> AiSuggestions =
	[
		"Which checks have the most flagged rows?",
		"Summarize the round-off alerts against the configured limit.",
		"Which transactions are missing an accounting posting?",
		"What possible duplicates should I review first?"
	];

	private bool _showAi = false;

	private void ToggleAi() => _showAi = !_showAi;

	// This reflects the currently loaded report period and its already-computed alert lists.
	private string BuildAiContext()
	{
		static string N(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture);
		static string Clean(string value) => (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Replace('|', '/');
		static string Ref(string no, DateTime date, string location, decimal total) =>
			$"{date:dd-MMM-yyyy HH:mm} | {Clean(no)} | {Clean(location)} | total ₹{N(total)}";
		static string TransferLocation(string from, string to) => $"{Clean(from)} to {Clean(to)}";
		string OutletDiscount(int? ledgerId) => GetOutletByLedger(ledgerId) is { } outlet
			? $"configured outlet discount {N(outlet.Discount)}%"
			: "configured outlet discount unavailable";
		string TransferDiscount(int locationId) => $"configured destination discount {N(_locations.FirstOrDefault(l => l.Id == locationId)?.Discount ?? 0)}%";

		var sb = new StringBuilder();
		sb.AppendLine($"Period: {_fromDate:dd-MMM-yyyy} to {_toDate:dd-MMM-yyyy}");
		sb.AppendLine($"As of: {_currentDateTime:dd-MMM-yyyy HH:mm}");
		if (_toDate.Date >= _currentDateTime.Date)
			sb.AppendLine("The latest day in the period is incomplete.");
		sb.AppendLine($"Maximum customer discount: {_customerMaxDiscountThreshold}% (flags discounts strictly above this value).");
		sb.AppendLine($"Maximum round-off amount: ₹{N(_maxRoundOffAmount)} (flags absolute amounts strictly above this value).");
		sb.AppendLine("Counts below are flagged rows, not unique transactions. Up to 8 example references are shown per check.");
		sb.AppendLine();

		void AddCategory<T>(string title, IReadOnlyCollection<T> rows, Func<T, string> describe)
		{
			sb.AppendLine($"{title}: {rows.Count} flagged rows");
			foreach (var row in rows.Take(8))
				sb.AppendLine($"- {describe(row)}");
			if (rows.Count > 8)
				sb.AppendLine($"- {rows.Count - 8} additional rows omitted from examples");
			sb.AppendLine();
		}

		AddCategory("Main outlet sale discount differs from outlet setting", _fallacyDiscountMainOutletSaleOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount)} | applied discount {N(t.DiscountPercent)}% ({N(t.DiscountAmount)}) | {OutletDiscount(t.PartyId)}");
		AddCategory("Main outlet sale return discount differs from outlet setting", _fallacyDiscountMainOutletSaleReturnOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount)} | applied discount {N(t.DiscountPercent)}% ({N(t.DiscountAmount)}) | {OutletDiscount(t.PartyId)}");
		AddCategory("Main outlet stock transfer discount differs from destination outlet setting", _fallacyDiscountMainOutletStockTransferOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, TransferLocation(t.LocationName, t.ToLocationName), t.TotalAmount)} | applied discount {N(t.DiscountPercent)}% ({N(t.DiscountAmount)}) | {TransferDiscount(t.ToLocationId)}");
		AddCategory("Outlet customer sale discount above maximum", _fallacyDiscountOutletCustomerSaleOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount)} | discount {N(t.DiscountPercent)}% ({N(t.DiscountAmount)})");
		AddCategory("Outlet customer sale return discount above maximum", _fallacyDiscountOutletCustomerSaleReturnOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount)} | discount {N(t.DiscountPercent)}% ({N(t.DiscountAmount)})");
		AddCategory("Bill discount above maximum", _fallacyDiscountCustomerBillOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount)} | discount {N(t.DiscountPercent)}% ({N(t.DiscountAmount)})");

		AddCategory("Missing accounting posting: sale", _fallacyMissingPostingSaleOverviews,
			t => Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount));
		AddCategory("Missing accounting posting: sale return", _fallacyMissingPostingSaleReturnOverviews,
			t => Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount));
		AddCategory("Missing accounting posting: stock transfer", _fallacyMissingPostingStockTransferOverviews,
			t => Ref(t.TransactionNo, t.TransactionDateTime, TransferLocation(t.LocationName, t.ToLocationName), t.TotalAmount));
		AddCategory("Missing accounting posting: bill", _fallacyMissingPostingBillOverviews,
			t => Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount));

		AddCategory("Round-off above limit: sale", _fallacyRoundOffSaleOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount)} | round-off ₹{N(t.RoundOffAmount)}");
		AddCategory("Round-off above limit: sale return", _fallacyRoundOffSaleReturnOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount)} | round-off ₹{N(t.RoundOffAmount)}");
		AddCategory("Round-off above limit: stock transfer", _fallacyRoundOffStockTransferOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, TransferLocation(t.LocationName, t.ToLocationName), t.TotalAmount)} | round-off ₹{N(t.RoundOffAmount)}");
		AddCategory("Round-off above limit: bill", _fallacyRoundOffBillOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount)} | round-off ₹{N(t.RoundOffAmount)}");

		AddCategory("Possible duplicate sale", _fallacyDuplicateSaleOverviews,
			t => Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount));
		AddCategory("Possible duplicate sale return", _fallacyDuplicateSaleReturnOverviews,
			t => Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount));
		AddCategory("Possible duplicate stock transfer", _fallacyDuplicateStockTransferOverviews,
			t => Ref(t.TransactionNo, t.TransactionDateTime, TransferLocation(t.LocationName, t.ToLocationName), t.TotalAmount));
		AddCategory("Possible duplicate bill", _fallacyDuplicateBillOverviews,
			t => Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount));

		AddCategory("Credit on outlet customer sale", _fallacyCreditOutletCustomerSaleOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount)} | credit ₹{N(t.Credit)}");
		AddCategory("Credit on bill", _fallacyCreditCustomerBillOverviews,
			t => $"{Ref(t.TransactionNo, t.TransactionDateTime, t.LocationName, t.TotalAmount)} | credit ₹{N(t.Credit)}");

		return sb.ToString();
	}
}

using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Data.Accounts.FinancialAccounting;

public static class FinancialAccountingData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(FinancialAccountingData));

	public static async Task<List<TrialBalanceModel>> LoadTrialBalanceByCompanyDate(int CompanyId, DateTime StartDate, DateTime EndDate) =>
		await ApiClient.Get<List<TrialBalanceModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadTrialBalanceByCompanyDate)), new { CompanyId, StartDate, EndDate });

	public static async Task<FinancialAccountingInvoiceBundle> LoadInvoiceBundle(int transactionId) =>
		await ApiClient.Get<FinancialAccountingInvoiceBundle>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadInvoiceBundle)), new { transactionId });

	public static async Task DeleteTransaction(FinancialAccountingModel accounting) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), accounting);

	public static async Task RecoverTransaction(FinancialAccountingModel accounting) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), accounting);

	public static async Task<int> SaveTransaction(FinancialAccountingModel accounting, List<FinancialAccountingLedgerModel> ledgers, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new FinancialAccountingSaveRequest(accounting, ledgers, recover));

	public static async Task SaveBRSDates(List<FinancialAccountingLedgerModel> changedLines, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveBRSDates)), changedLines, new { userId, formFactor, platform, latitude, longitude });
}

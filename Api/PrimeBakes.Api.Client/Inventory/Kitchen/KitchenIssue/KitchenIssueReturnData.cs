using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;

namespace PrimeBakes.Data.Inventory.Kitchen.KitchenIssue;

public static class KitchenIssueReturnData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenIssueReturnData));

	public static async Task<KitchenIssueReturnInvoiceBundle> LoadInvoiceBundle(int transactionId) =>
		await ApiClient.Get<KitchenIssueReturnInvoiceBundle>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadInvoiceBundle)), new { transactionId });


	public static async Task DeleteTransaction(KitchenIssueReturnModel kitchenIssueReturn) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), kitchenIssueReturn);

	public static async Task RecoverTransaction(KitchenIssueReturnModel kitchenIssueReturn) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), kitchenIssueReturn);

	public static async Task<int> SaveTransaction(KitchenIssueReturnModel kitchenIssueReturn, List<KitchenIssueReturnDetailModel> kitchenIssueReturnDetails, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new KitchenIssueReturnSaveRequest(kitchenIssueReturn, kitchenIssueReturnDetails, recover));
}

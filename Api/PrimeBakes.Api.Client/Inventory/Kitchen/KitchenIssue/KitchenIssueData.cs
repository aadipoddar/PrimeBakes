using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;

namespace PrimeBakes.Data.Inventory.Kitchen.KitchenIssue;

public static class KitchenIssueData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenIssueData));

	public static async Task<KitchenIssueInvoiceBundle> LoadInvoiceBundle(int transactionId) =>
		await ApiClient.Get<KitchenIssueInvoiceBundle>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadInvoiceBundle)), new { transactionId });


	public static async Task DeleteTransaction(KitchenIssueModel kitchenIssue) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), kitchenIssue);

	public static async Task RecoverTransaction(KitchenIssueModel kitchenIssue) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), kitchenIssue);

	public static async Task<int> SaveTransaction(KitchenIssueModel kitchenIssue, List<KitchenIssueDetailModel> kitchenIssueDetails, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new KitchenIssueSaveRequest(kitchenIssue, kitchenIssueDetails, recover));
}

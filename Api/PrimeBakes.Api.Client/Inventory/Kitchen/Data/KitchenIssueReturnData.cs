using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Library.Inventory.Kitchen.Data;

public static class KitchenIssueReturnData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(KitchenIssueReturnData));

	public static async Task DeleteTransaction(KitchenIssueReturnModel kitchenIssueReturn) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), kitchenIssueReturn);

	public static async Task RecoverTransaction(KitchenIssueReturnModel kitchenIssueReturn) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), kitchenIssueReturn);

	public static async Task<int> SaveTransaction(KitchenIssueReturnModel kitchenIssueReturn, List<KitchenIssueReturnDetailModel> kitchenIssueReturnDetails, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new KitchenIssueReturnSaveRequest(kitchenIssueReturn, kitchenIssueReturnDetails, recover));
}

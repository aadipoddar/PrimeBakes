using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Bill;
using PrimeBakes.Models.Store.Customer;

namespace PrimeBakes.Library.Restaurant.Bill.Data;

public static class BillData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(BillData));

	public static async Task<List<BillModel>> LoadRunningBillByLocationId(int LocationId) =>
		await ApiClient.Get<List<BillModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadRunningBillByLocationId)), new { LocationId });

	public static async Task<Dictionary<int, List<BillItemCartModel>>> KOTCategoryItemsFromBill(int billId) =>
		await ApiClient.Get<Dictionary<int, List<BillItemCartModel>>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(KOTCategoryItemsFromBill)), new { billId });

	public static async Task MarkKOTAsPrinted(int billId) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(MarkKOTAsPrinted)), new { }, new { billId });

	public static async Task DeleteTransaction(BillModel bill) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), bill);

	public static async Task RecoverTransaction(BillModel bill) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), bill);

	public static async Task<int> SaveTransaction(BillModel bill, List<BillDetailModel> billDetails, CustomerModel customer = null, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new BillSaveRequest(bill, billDetails, customer, recover));

	public static async Task PostDayBills(DateTime postingDate, int locationId, int userId, string userPlatform) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(PostDayBills)), new { }, new { postingDate, locationId, userId, userPlatform });

}

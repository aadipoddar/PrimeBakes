using PrimeBakes.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Order;

namespace PrimeBakes.Data.Store.Order;

public static class OrderData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(OrderData));


	public static async Task<List<OrderModel>> LoadOrderByLocationPending(int LocationId) =>
		await ApiClient.Get<List<OrderModel>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadOrderByLocationPending)), new { LocationId });

	public static async Task<OrderInvoiceBundle> LoadInvoiceBundle(int transactionId) =>
		await ApiClient.Get<OrderInvoiceBundle>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadInvoiceBundle)), new { transactionId });

	public static async Task LinkOrderToSale(int? orderId = null, int? saleId = null, bool unlink = false) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LinkOrderToSale)), new { }, new { orderId, saleId, unlink });

	public static async Task DeleteTransaction(OrderModel order) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(DeleteTransaction)), order);

	public static async Task RecoverTransaction(OrderModel order) =>
		await ApiClient.Post(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(RecoverTransaction)), order);

	public static async Task<int> SaveTransaction(OrderModel order, List<OrderDetailModel> orderDetails, bool recover = false) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(SaveTransaction)),
			new OrderSaveRequest(order, orderDetails, recover));
}

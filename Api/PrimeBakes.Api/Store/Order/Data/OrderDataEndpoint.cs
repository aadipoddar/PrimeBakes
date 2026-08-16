using PrimeBakes.Library.Store.Order.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Order;

namespace PrimeBakes.Api.Store.Order.Data;

public class OrderDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(OrderDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(OrderData.LoadOrderByLocationPending),
			(int LocationId) => OrderData.LoadOrderByLocationPending(LocationId));

		group.MapPost(nameof(OrderData.LinkOrderToSale),
			(int? orderId, int? saleId, bool unlink) => OrderData.LinkOrderToSale(orderId, saleId, unlink));

		group.MapPost(nameof(OrderData.DeleteTransaction), (OrderModel order) => OrderData.DeleteTransaction(order));
		group.MapPost(nameof(OrderData.RecoverTransaction), (OrderModel order) => OrderData.RecoverTransaction(order));
		group.MapPost(nameof(OrderData.SaveTransaction), (OrderSaveRequest request) => OrderData.SaveTransaction(request.Order, request.OrderDetails, request.Recover));
	}
}

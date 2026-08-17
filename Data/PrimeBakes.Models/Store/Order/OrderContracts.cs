using PrimeBakes.Models.Accounts.Masters;

namespace PrimeBakes.Models.Store.Order;

public sealed record OrderSaveRequest(
	OrderModel Order,
	List<OrderDetailModel> OrderDetails,
	bool Recover);

public sealed record OrderInvoiceBundle(
	OrderOverviewModel Transaction,
	List<OrderItemOverviewModel> Details,
	CompanyModel Company,
	LedgerModel LocationLedger,
	DateTime CurrentDateTime);

public static class OrderCartExtensions
{
	public static List<OrderDetailModel> ConvertCartToDetails(this List<OrderItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new OrderDetailModel
		{
			Id = 0,
			MasterId = masterId,
			ProductId = item.ItemId,
			Quantity = item.Quantity,
			Remarks = item.Remarks,
			Status = true
		})];
}

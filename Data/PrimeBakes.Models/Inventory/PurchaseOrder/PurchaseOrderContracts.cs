using PrimeBakes.Models.Accounts.Masters;

namespace PrimeBakes.Models.Inventory.PurchaseOrder;

public sealed record PurchaseOrderSaveRequest(
	PurchaseOrderModel PurchaseOrder,
	List<PurchaseOrderDetailModel> Details,
	bool Recover);

public sealed record PurchaseOrderInvoiceBundle(
	PurchaseOrderOverviewModel Transaction,
	List<PurchaseOrderItemOverviewModel> Details,
	CompanyModel Company,
	LedgerModel Party,
	DateTime CurrentDateTime);

public static class PurchaseOrderCartExtensions
{
	public static List<PurchaseOrderDetailModel> ConvertCartToDetails(this List<PurchaseOrderItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new PurchaseOrderDetailModel
		{
			Id = 0,
			MasterId = masterId,
			RawMaterialId = item.ItemId,
			Quantity = item.Quantity,
			UnitOfMeasurement = item.UnitOfMeasurement,
			Remarks = item.Remarks,
			Status = true
		})];
}

using PrimeBakes.Models.Accounts.Masters;

namespace PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;

public sealed record KitchenProductionSaveRequest(
	KitchenProductionModel KitchenProduction,
	List<KitchenProductionDetailModel> Details,
	bool Recover);

public sealed record KitchenProductionReturnSaveRequest(
	KitchenProductionReturnModel KitchenProductionReturn,
	List<KitchenProductionReturnDetailModel> Details,
	bool Recover);

public sealed record KitchenProductionInvoiceBundle(
	KitchenProductionOverviewModel Transaction,
	List<KitchenProductionItemOverviewModel> Details,
	CompanyModel Company,
	KitchenModel Kitchen,
	DateTime CurrentDateTime);

public sealed record KitchenProductionReturnInvoiceBundle(
	KitchenProductionReturnOverviewModel Transaction,
	List<KitchenProductionReturnItemOverviewModel> Details,
	CompanyModel Company,
	KitchenModel Kitchen,
	DateTime CurrentDateTime);

public static class KitchenProductionCartExtensions
{
	public static List<KitchenProductionDetailModel> ConvertCartToDetails(this List<KitchenProductionProductCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new KitchenProductionDetailModel
		{
			Id = 0,
			MasterId = masterId,
			ProductId = item.ProductId,
			Quantity = item.Quantity,
			Rate = item.Rate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];
}

public static class KitchenProductionReturnCartExtensions
{
	public static List<KitchenProductionReturnDetailModel> ConvertCartToDetails(this List<KitchenProductionReturnProductCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new KitchenProductionReturnDetailModel
		{
			Id = 0,
			MasterId = masterId,
			ProductId = item.ProductId,
			Quantity = item.Quantity,
			Rate = item.Rate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];
}

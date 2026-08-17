using PrimeBakes.Models.Accounts.Masters;

namespace PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;

public sealed record KitchenIssueSaveRequest(
	KitchenIssueModel KitchenIssue,
	List<KitchenIssueDetailModel> Details,
	bool Recover);

public sealed record KitchenIssueReturnSaveRequest(
	KitchenIssueReturnModel KitchenIssueReturn,
	List<KitchenIssueReturnDetailModel> Details,
	bool Recover);

public sealed record KitchenIssueInvoiceBundle(
	KitchenIssueOverviewModel Transaction,
	List<KitchenIssueItemOverviewModel> Details,
	CompanyModel Company,
	KitchenModel Kitchen,
	DateTime CurrentDateTime);

public sealed record KitchenIssueReturnInvoiceBundle(
	KitchenIssueReturnOverviewModel Transaction,
	List<KitchenIssueReturnItemOverviewModel> Details,
	CompanyModel Company,
	KitchenModel Kitchen,
	DateTime CurrentDateTime);

public static class KitchenIssueCartExtensions
{
	public static List<KitchenIssueDetailModel> ConvertCartToDetails(this List<KitchenIssueItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new KitchenIssueDetailModel
		{
			Id = 0,
			MasterId = masterId,
			RawMaterialId = item.ItemId,
			Quantity = item.Quantity,
			UnitOfMeasurement = item.UnitOfMeasurement,
			Rate = item.Rate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];
}

public static class KitchenIssueReturnCartExtensions
{
	public static List<KitchenIssueReturnDetailModel> ConvertCartToDetails(this List<KitchenIssueReturnItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new KitchenIssueReturnDetailModel
		{
			Id = 0,
			MasterId = masterId,
			RawMaterialId = item.ItemId,
			Quantity = item.Quantity,
			UnitOfMeasurement = item.UnitOfMeasurement,
			Rate = item.Rate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];
}

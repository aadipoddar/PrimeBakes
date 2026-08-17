using PrimeBakes.Models.Accounts.Masters;

namespace PrimeBakes.Models.Inventory.Purchase;

public sealed record PurchaseSaveRequest(
	PurchaseModel Purchase,
	List<PurchaseDetailModel> Details,
	bool Recover);

public sealed record PurchaseReturnSaveRequest(
	PurchaseReturnModel PurchaseReturn,
	List<PurchaseReturnDetailModel> Details,
	bool Recover);

public sealed record PurchaseInvoiceBundle(
	PurchaseOverviewModel Transaction,
	List<PurchaseItemOverviewModel> Details,
	CompanyModel Company,
	LedgerModel Party,
	DateTime CurrentDateTime);

public sealed record PurchaseReturnInvoiceBundle(
	PurchaseReturnOverviewModel Transaction,
	List<PurchaseReturnItemOverviewModel> Details,
	CompanyModel Company,
	LedgerModel Party,
	DateTime CurrentDateTime);

public static class PurchaseCartExtensions
{
	public static List<PurchaseDetailModel> ConvertCartToDetails(this List<PurchaseItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new PurchaseDetailModel
		{
			Id = 0,
			MasterId = masterId,
			RawMaterialId = item.ItemId,
			Quantity = item.Quantity,
			UnitOfMeasurement = item.UnitOfMeasurement,
			Rate = item.Rate,
			BaseTotal = item.BaseTotal,
			DiscountPercent = item.DiscountPercent,
			DiscountAmount = item.DiscountAmount,
			AfterDiscount = item.AfterDiscount,
			CGSTPercent = item.CGSTPercent,
			CGSTAmount = item.CGSTAmount,
			SGSTPercent = item.SGSTPercent,
			SGSTAmount = item.SGSTAmount,
			IGSTPercent = item.IGSTPercent,
			IGSTAmount = item.IGSTAmount,
			TotalTaxAmount = item.TotalTaxAmount,
			InclusiveTax = item.InclusiveTax,
			NetRate = item.NetRate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];
}

public static class PurchaseReturnCartExtensions
{
	public static List<PurchaseReturnDetailModel> ConvertCartToDetails(this List<PurchaseReturnItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new PurchaseReturnDetailModel
		{
			Id = 0,
			MasterId = masterId,
			RawMaterialId = item.ItemId,
			Quantity = item.Quantity,
			UnitOfMeasurement = item.UnitOfMeasurement,
			Rate = item.Rate,
			BaseTotal = item.BaseTotal,
			DiscountPercent = item.DiscountPercent,
			DiscountAmount = item.DiscountAmount,
			AfterDiscount = item.AfterDiscount,
			CGSTPercent = item.CGSTPercent,
			CGSTAmount = item.CGSTAmount,
			SGSTPercent = item.SGSTPercent,
			SGSTAmount = item.SGSTAmount,
			IGSTPercent = item.IGSTPercent,
			IGSTAmount = item.IGSTAmount,
			TotalTaxAmount = item.TotalTaxAmount,
			InclusiveTax = item.InclusiveTax,
			NetRate = item.NetRate,
			Total = item.Total,
			Remarks = item.Remarks,
			Status = true
		})];
}

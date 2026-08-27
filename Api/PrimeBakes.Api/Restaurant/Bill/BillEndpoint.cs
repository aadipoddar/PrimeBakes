using PrimeBakes.Data.Restaurant.Bill;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Bill;

namespace PrimeBakes.Api.Restaurant.Bill;

public class BillEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(BillEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(BillData.LoadRunningBillByLocationId),
			(int LocationId) => BillData.LoadRunningBillByLocationId(LocationId));

		group.MapGet(nameof(BillData.KOTCategoryItemsFromBill),
			(int billId) => BillData.KOTCategoryItemsFromBill(billId));

		group.MapGet(nameof(BillData.LoadInvoiceBundle),
			(int transactionId) => BillData.LoadInvoiceBundle(transactionId));

		group.MapGet(nameof(BillData.LoadThermalBundle),
			(int billId) => BillData.LoadThermalBundle(billId));

		group.MapGet(nameof(BillData.LoadKOTThermalBundle),
			(int billId, int kotCategoryId) => BillData.LoadKOTThermalBundle(billId, kotCategoryId));


		group.MapPost(nameof(BillData.MarkKOTAsPrinted),
			(int billId) => BillData.MarkKOTAsPrinted(billId));

		group.MapPost(nameof(BillData.DeleteTransaction),
			(BillModel bill) => BillData.DeleteTransaction(bill));

		group.MapPost(nameof(BillData.RecoverTransaction),
			(BillModel bill) => BillData.RecoverTransaction(bill));

		group.MapPost(nameof(BillData.SaveTransaction),
			(BillSaveRequest request) => BillData.SaveTransaction(request.Bill, request.BillDetails, request.Customer, request.Recover));

		group.MapPost(nameof(BillData.PostDayBills),
			(DateTime postingDate, int locationId, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
				BillData.PostDayBills(postingDate, locationId, userId, formFactor, platform, latitude, longitude));
	}
}

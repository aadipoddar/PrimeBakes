using PrimeBakes.Library.Restaurant.Bill.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Bill;

namespace PrimeBakes.Api.Restaurant.Bill.Data;

public class BillDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(BillDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(BillData.LoadRunningBillByLocationId),
			(int LocationId) => BillData.LoadRunningBillByLocationId(LocationId));

		group.MapGet(nameof(BillData.KOTCategoryItemsFromBill),
			(int billId) => BillData.KOTCategoryItemsFromBill(billId));

		group.MapPost(nameof(BillData.MarkKOTAsPrinted),
			(int billId) => BillData.MarkKOTAsPrinted(billId));

		group.MapPost(nameof(BillData.DeleteTransaction),
			(BillModel bill) => BillData.DeleteTransaction(bill));

		group.MapPost(nameof(BillData.RecoverTransaction),
			(BillModel bill) => BillData.RecoverTransaction(bill));

		group.MapPost(nameof(BillData.SaveTransaction),
			(BillSaveRequest request) => BillData.SaveTransaction(request.Bill, request.BillDetails, request.Customer, request.Recover));

		group.MapPost(nameof(BillData.PostDayBills),
			(DateTime postingDate, int locationId, int userId, string userPlatform) => BillData.PostDayBills(postingDate, locationId, userId, userPlatform));
	}
}

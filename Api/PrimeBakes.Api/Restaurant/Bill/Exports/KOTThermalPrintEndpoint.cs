using PrimeBakes.Library.Restaurant.Bill.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Bill;

namespace PrimeBakes.Api.Restaurant.Bill.Exports;

public class KOTThermalPrintEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KOTThermalPrintEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KOTThermalPrint.GenerateThermalBill),
			(KOTThermalRequest request) => KOTThermalPrint.GenerateThermalBill(request.BillId, request.KotCategoryId, request.KotItems));

		group.MapPost(nameof(KOTThermalPrint.GenerateThermalBillPng),
			(KOTThermalRequest request) => KOTThermalPrint.GenerateThermalBillPng(request.BillId, request.KotCategoryId, request.KotItems));
	}
}

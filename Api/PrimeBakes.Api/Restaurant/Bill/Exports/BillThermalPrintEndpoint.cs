using PrimeBakes.Library.Restaurant.Bill.Exports;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Restaurant.Bill.Exports;

public class BillThermalPrintEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(BillThermalPrintEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(BillThermalPrint.GenerateThermalBill),
			(int billId) => BillThermalPrint.GenerateThermalBill(billId));

		group.MapGet(nameof(BillThermalPrint.GenerateThermalBillPng),
			(int billId) => BillThermalPrint.GenerateThermalBillPng(billId));
	}
}

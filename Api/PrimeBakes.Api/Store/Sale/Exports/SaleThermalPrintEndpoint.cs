using PrimeBakes.Library.Store.Sale.Exports;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Store.Sale.Exports;

public class SaleThermalPrintEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(SaleThermalPrintEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(SaleThermalPrint.GenerateThermalBill), (int saleId) => SaleThermalPrint.GenerateThermalBill(saleId));
		group.MapGet(nameof(SaleThermalPrint.GenerateThermalBillPng), (int saleId) => SaleThermalPrint.GenerateThermalBillPng(saleId));
	}
}

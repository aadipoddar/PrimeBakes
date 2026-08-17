using PrimeBakes.Data.Store.Sale;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Api.Store.Sale;

public class SaleEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(SaleEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(SaleData.LoadInvoiceBundle),
			(int transactionId) => SaleData.LoadInvoiceBundle(transactionId));

		group.MapGet(nameof(SaleData.LoadThermalBundle),
			(int saleId) => SaleData.LoadThermalBundle(saleId));


		group.MapPost(nameof(SaleData.PostDaySales),
			(DateTime postingDate, int locationId, int userId, string userPlatform) => SaleData.PostDaySales(postingDate, locationId, userId, userPlatform));

		group.MapPost(nameof(SaleData.DeleteTransaction), (SaleModel sale) => SaleData.DeleteTransaction(sale));
		group.MapPost(nameof(SaleData.RecoverTransaction), (SaleModel sale) => SaleData.RecoverTransaction(sale));
		group.MapPost(nameof(SaleData.SaveTransaction), (SaleSaveRequest request) => SaleData.SaveTransaction(request.Sale, request.SaleDetails, request.Customer, request.Recover));
	}
}

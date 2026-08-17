using PrimeBakes.Data.Store.Sale;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Api.Store.Sale;

public class SaleReturnEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(SaleReturnEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(SaleReturnData.LoadInvoiceBundle),
			(int transactionId) => SaleReturnData.LoadInvoiceBundle(transactionId));


		group.MapPost(nameof(SaleReturnData.DeleteTransaction), (SaleReturnModel saleReturn) => SaleReturnData.DeleteTransaction(saleReturn));
		group.MapPost(nameof(SaleReturnData.RecoverTransaction), (SaleReturnModel saleReturn) => SaleReturnData.RecoverTransaction(saleReturn));
		group.MapPost(nameof(SaleReturnData.SaveTransaction), (SaleReturnSaveRequest request) => SaleReturnData.SaveTransaction(request.SaleReturn, request.SaleReturnDetails, request.Customer, request.Recover));
	}
}

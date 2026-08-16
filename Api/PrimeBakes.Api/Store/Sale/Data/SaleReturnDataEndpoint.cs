using PrimeBakes.Library.Store.Sale.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.Sale;

namespace PrimeBakes.Api.Store.Sale.Data;

public class SaleReturnDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(SaleReturnDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(SaleReturnData.DeleteTransaction), (SaleReturnModel saleReturn) => SaleReturnData.DeleteTransaction(saleReturn));
		group.MapPost(nameof(SaleReturnData.RecoverTransaction), (SaleReturnModel saleReturn) => SaleReturnData.RecoverTransaction(saleReturn));
		group.MapPost(nameof(SaleReturnData.SaveTransaction), (SaleReturnSaveRequest request) => SaleReturnData.SaveTransaction(request.SaleReturn, request.SaleReturnDetails, request.Customer, request.Recover));
	}
}

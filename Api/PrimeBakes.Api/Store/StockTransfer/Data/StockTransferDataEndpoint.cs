using PrimeBakes.Library.Store.StockTransfer.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.StockTransfer;

namespace PrimeBakes.Api.Store.StockTransfer.Data;

public class StockTransferDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(StockTransferDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(StockTransferData.DeleteTransaction), (StockTransferModel stockTransfer) => StockTransferData.DeleteTransaction(stockTransfer));
		group.MapPost(nameof(StockTransferData.RecoverTransaction), (StockTransferModel stockTransfer) => StockTransferData.RecoverTransaction(stockTransfer));
		group.MapPost(nameof(StockTransferData.SaveTransaction), (StockTransferSaveRequest request) => StockTransferData.SaveTransaction(request.StockTransfer, request.StockTransferDetails, request.Recover));
	}
}

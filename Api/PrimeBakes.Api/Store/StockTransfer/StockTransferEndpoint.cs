using PrimeBakes.Data.Store.StockTransfer;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Store.StockTransfer;

namespace PrimeBakes.Api.Store.StockTransfer;

public class StockTransferEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(StockTransferEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(StockTransferData.LoadInvoiceBundle),
			(int transactionId) => StockTransferData.LoadInvoiceBundle(transactionId));


		group.MapPost(nameof(StockTransferData.DeleteTransaction), (StockTransferModel stockTransfer) => StockTransferData.DeleteTransaction(stockTransfer));
		group.MapPost(nameof(StockTransferData.RecoverTransaction), (StockTransferModel stockTransfer) => StockTransferData.RecoverTransaction(stockTransfer));
		group.MapPost(nameof(StockTransferData.SaveTransaction), (StockTransferSaveRequest request) => StockTransferData.SaveTransaction(request.StockTransfer, request.StockTransferDetails, request.Recover));
	}
}

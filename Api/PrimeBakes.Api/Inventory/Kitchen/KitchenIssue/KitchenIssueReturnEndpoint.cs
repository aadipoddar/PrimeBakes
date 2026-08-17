using PrimeBakes.Data.Inventory.Kitchen;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;

namespace PrimeBakes.Api.Inventory.Kitchen;

public class KitchenIssueReturnEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenIssueReturnEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(KitchenIssueReturnData.LoadInvoiceBundle),
			(int transactionId) => KitchenIssueReturnData.LoadInvoiceBundle(transactionId));


		group.MapPost(nameof(KitchenIssueReturnData.DeleteTransaction), (KitchenIssueReturnModel kitchenIssueReturn) => KitchenIssueReturnData.DeleteTransaction(kitchenIssueReturn));
		group.MapPost(nameof(KitchenIssueReturnData.RecoverTransaction), (KitchenIssueReturnModel kitchenIssueReturn) => KitchenIssueReturnData.RecoverTransaction(kitchenIssueReturn));
		group.MapPost(nameof(KitchenIssueReturnData.SaveTransaction), (KitchenIssueReturnSaveRequest request) => KitchenIssueReturnData.SaveTransaction(request.KitchenIssueReturn, request.Details, request.Recover));
	}
}

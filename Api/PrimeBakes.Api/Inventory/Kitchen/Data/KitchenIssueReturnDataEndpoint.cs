using PrimeBakes.Library.Inventory.Kitchen.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Api.Inventory.Kitchen.Data;

public class KitchenIssueReturnDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenIssueReturnDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KitchenIssueReturnData.DeleteTransaction), (KitchenIssueReturnModel kitchenIssueReturn) => KitchenIssueReturnData.DeleteTransaction(kitchenIssueReturn));
		group.MapPost(nameof(KitchenIssueReturnData.RecoverTransaction), (KitchenIssueReturnModel kitchenIssueReturn) => KitchenIssueReturnData.RecoverTransaction(kitchenIssueReturn));
		group.MapPost(nameof(KitchenIssueReturnData.SaveTransaction), (KitchenIssueReturnSaveRequest request) => KitchenIssueReturnData.SaveTransaction(request.KitchenIssueReturn, request.Details, request.Recover));
	}
}

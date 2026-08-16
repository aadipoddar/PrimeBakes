using PrimeBakes.Library.Inventory.Kitchen.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;

namespace PrimeBakes.Api.Inventory.Kitchen.Data;

public class KitchenIssueDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenIssueDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KitchenIssueData.DeleteTransaction), (KitchenIssueModel kitchenIssue) => KitchenIssueData.DeleteTransaction(kitchenIssue));
		group.MapPost(nameof(KitchenIssueData.RecoverTransaction), (KitchenIssueModel kitchenIssue) => KitchenIssueData.RecoverTransaction(kitchenIssue));
		group.MapPost(nameof(KitchenIssueData.SaveTransaction), (KitchenIssueSaveRequest request) => KitchenIssueData.SaveTransaction(request.KitchenIssue, request.Details, request.Recover));
	}
}

using PrimeBakes.Data.Inventory.Kitchen;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;

namespace PrimeBakes.Api.Inventory.Kitchen;

public class KitchenIssueEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KitchenIssueEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(KitchenIssueData.LoadInvoiceBundle),
			(int transactionId) => KitchenIssueData.LoadInvoiceBundle(transactionId));


		group.MapPost(nameof(KitchenIssueData.DeleteTransaction), (KitchenIssueModel kitchenIssue) => KitchenIssueData.DeleteTransaction(kitchenIssue));
		group.MapPost(nameof(KitchenIssueData.RecoverTransaction), (KitchenIssueModel kitchenIssue) => KitchenIssueData.RecoverTransaction(kitchenIssue));
		group.MapPost(nameof(KitchenIssueData.SaveTransaction), (KitchenIssueSaveRequest request) => KitchenIssueData.SaveTransaction(request.KitchenIssue, request.Details, request.Recover));
	}
}

using PrimeBakes.Library.Restaurant.Dining.Data;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Restaurant.Dining;

namespace PrimeBakes.Api.Restaurant.Dining.Data;

public class DiningTableDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(DiningTableDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(DiningTableData.InsertDiningTable), (DiningTableModel diningTable) => DiningTableData.InsertDiningTable(diningTable));
		group.MapPost(nameof(DiningTableData.DeleteTransaction), DiningTableData.DeleteTransaction);
		group.MapPost(nameof(DiningTableData.RecoverTransaction), DiningTableData.RecoverTransaction);
		group.MapPost(nameof(DiningTableData.SaveTransaction), DiningTableData.SaveTransaction);
	}
}

using PrimeBakes.Library.Accounts.Masters.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Api.Accounts.Masters.Exports;

public class GroupExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(GroupExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(GroupExport.ExportMaster), async (List<GroupModel> groupData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await GroupExport.ExportMaster(groupData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

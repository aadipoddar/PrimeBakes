using PrimeBakes.Library.Accounts.Masters.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Api.Accounts.Masters.Exports;

public class StateUTExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(StateUTExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(StateUTExport.ExportMaster), async (List<StateUTModel> stateUTData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await StateUTExport.ExportMaster(stateUTData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

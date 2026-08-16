using PrimeBakes.Library.Operations.Location;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.Location;

namespace PrimeBakes.Api.Operations.Location;

public class LocationExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(LocationExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(LocationExport.ExportMaster), async (List<LocationModel> locationData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await LocationExport.ExportMaster(locationData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

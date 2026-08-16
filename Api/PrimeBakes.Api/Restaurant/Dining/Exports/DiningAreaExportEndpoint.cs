using PrimeBakes.Library.Restaurant.Dining.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Restaurant.Dining;

namespace PrimeBakes.Api.Restaurant.Dining.Exports;

public class DiningAreaExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(DiningAreaExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(DiningAreaExport.ExportMaster), async (List<DiningAreaModel> diningAreaData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await DiningAreaExport.ExportMaster(diningAreaData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

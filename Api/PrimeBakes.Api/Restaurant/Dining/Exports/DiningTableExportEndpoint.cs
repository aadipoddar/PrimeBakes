using PrimeBakes.Library.Restaurant.Dining.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Restaurant.Dining;

namespace PrimeBakes.Api.Restaurant.Dining.Exports;

public class DiningTableExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(DiningTableExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(DiningTableExport.ExportMaster), async (List<DiningTableModel> diningTableData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await DiningTableExport.ExportMaster(diningTableData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

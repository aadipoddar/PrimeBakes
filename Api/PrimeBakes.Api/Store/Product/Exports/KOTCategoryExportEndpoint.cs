using PrimeBakes.Library.Store.Product.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Api.Store.Product.Exports;

public class KOTCategoryExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(KOTCategoryExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(KOTCategoryExport.ExportMaster), async (List<KOTCategoryModel> kotCategoryData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await KOTCategoryExport.ExportMaster(kotCategoryData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

using PrimeBakes.Library.Store.Product.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Api.Store.Product.Exports;

public class ProductLocationExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(ProductLocationExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(ProductLocationExport.ExportMaster), async (List<ProductLocationOverviewModel> productLocationData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await ProductLocationExport.ExportMaster(productLocationData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

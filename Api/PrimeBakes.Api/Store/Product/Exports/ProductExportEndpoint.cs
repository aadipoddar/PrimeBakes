using PrimeBakes.Library.Store.Product.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Api.Store.Product.Exports;

public class ProductExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(ProductExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(ProductExport.ExportMaster), async (List<ProductModel> productData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await ProductExport.ExportMaster(productData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

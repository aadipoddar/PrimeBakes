using PrimeBakes.Library.Store.Product.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Api.Store.Product.Exports;

public class ProductCategoryExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(ProductCategoryExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(ProductCategoryExport.ExportMaster), async (List<ProductCategoryModel> productCategoryData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await ProductCategoryExport.ExportMaster(productCategoryData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

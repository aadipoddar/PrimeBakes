using PrimeBakes.Library.Store.Product.Exports;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Store.Product;

namespace PrimeBakes.Api.Store.Product.Exports;

public class TaxExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(TaxExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(TaxExport.ExportMaster), async (List<TaxModel> taxData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await TaxExport.ExportMaster(taxData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

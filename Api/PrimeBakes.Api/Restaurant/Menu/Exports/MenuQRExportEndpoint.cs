using PrimeBakes.Library.Restaurant.Menu.Exports;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Restaurant.Menu.Exports;

public class MenuQRExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(MenuQRExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(MenuQRExport.ExportMenuQRCode), async (int locationId, string locationName) =>
		{
			var (stream, fileName) = await MenuQRExport.ExportMenuQRCode(locationId, locationName);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

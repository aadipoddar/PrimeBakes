using PrimeBakes.Api.Common;
using PrimeBakes.Data.Operations.Location;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Location;

namespace PrimeBakes.Api.Operations.Location;

public class LocationEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(LocationEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint).CacheOutput(ApiCachePolicy.Instance);

		group.MapGet(nameof(LocationData.LoadLedgerByLocationId),
			(int locationId) => LocationData.LoadLedgerByLocationId(locationId));

		group.MapPost(nameof(LocationData.DeleteTransaction), LocationData.DeleteTransaction);
		group.MapPost(nameof(LocationData.RecoverTransaction), LocationData.RecoverTransaction);

		group.MapPost(nameof(LocationData.SaveTransaction),
			(LocationSaveRequest request, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
				LocationData.SaveTransaction(request.Location, request.CopyLocation, userId, formFactor, platform, latitude, longitude));
	}
}

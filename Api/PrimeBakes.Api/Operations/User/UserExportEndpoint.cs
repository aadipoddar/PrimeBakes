using PrimeBakes.Library.Operations.User;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Api.Operations.User;

public class UserExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(UserExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(UserExport.ExportMaster), async (List<UserModel> userData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await UserExport.ExportMaster(userData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

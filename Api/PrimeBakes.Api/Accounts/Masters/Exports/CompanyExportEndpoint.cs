using PrimeBakes.Library.Accounts.Masters.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Api.Accounts.Masters.Exports;

public class CompanyExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(CompanyExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(CompanyExport.ExportMaster), async (List<CompanyModel> companyData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await CompanyExport.ExportMaster(companyData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

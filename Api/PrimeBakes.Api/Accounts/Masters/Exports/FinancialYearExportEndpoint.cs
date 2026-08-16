using PrimeBakes.Library.Accounts.Masters.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Api.Accounts.Masters.Exports;

public class FinancialYearExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(FinancialYearExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(FinancialYearExport.ExportMaster), async (List<FinancialYearModel> financialYearData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await FinancialYearExport.ExportMaster(financialYearData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

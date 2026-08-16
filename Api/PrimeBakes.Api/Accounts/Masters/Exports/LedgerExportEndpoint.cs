using PrimeBakes.Library.Accounts.Masters.Exports;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Exports;

namespace PrimeBakes.Api.Accounts.Masters.Exports;

public class LedgerExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(LedgerExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(LedgerExport.ExportMaster), async (List<LedgerModel> ledgerData, ReportExportType exportType) =>
		{
			var (stream, fileName) = await LedgerExport.ExportMaster(ledgerData, exportType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

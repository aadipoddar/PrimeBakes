using PrimeBakes.Library.Accounts.FinancialAccounting.Exports;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.FinancialAccounting.Exports;

public class TrialBalanceReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(TrialBalanceReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(TrialBalanceReportExport.ExportReport), async (TrialBalanceReportRequest request) =>
		{
			var (stream, fileName) = await TrialBalanceReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.Company, request.Group, request.AccountType);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

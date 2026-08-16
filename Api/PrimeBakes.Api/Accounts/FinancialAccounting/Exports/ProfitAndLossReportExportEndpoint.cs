using PrimeBakes.Library.Accounts.FinancialAccounting.Exports;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.FinancialAccounting.Exports;

public class ProfitAndLossReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(ProfitAndLossReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(ProfitAndLossReportExport.ExportIncomeReport), async (ProfitAndLossReportRequest request) =>
		{
			var (stream, fileName) = await ProfitAndLossReportExport.ExportIncomeReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.Company);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(ProfitAndLossReportExport.ExportExpenseReport), async (ProfitAndLossReportRequest request) =>
		{
			var (stream, fileName) = await ProfitAndLossReportExport.ExportExpenseReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.Company);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

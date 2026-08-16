using PrimeBakes.Library.Accounts.FinancialAccounting.Exports;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.FinancialAccounting.Exports;

public class FinancialAccountingReportExportEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(FinancialAccountingReportExportEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(FinancialAccountingReportExport.ExportReport), async (FinancialAccountingReportRequest request) =>
		{
			var (stream, fileName) = await FinancialAccountingReportExport.ExportReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.Company, request.Voucher);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});

		group.MapPost(nameof(FinancialAccountingReportExport.ExportLedgerReport), async (FinancialAccountingLedgerReportRequest request) =>
		{
			var (stream, fileName) = await FinancialAccountingReportExport.ExportLedgerReport(
				request.Data, request.ExportType, request.DateRangeStart, request.DateRangeEnd,
				request.ShowAllColumns, request.ShowDeleted, request.Company, request.Ledger, request.TrialBalance);
			return TypedResults.File(stream.ToArray(), Helper.ExportContentType, fileName);
		});
	}
}

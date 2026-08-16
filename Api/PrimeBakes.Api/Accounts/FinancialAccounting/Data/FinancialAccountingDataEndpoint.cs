using PrimeBakes.Library.Accounts.FinancialAccounting.Data;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.FinancialAccounting.Data;

public class FinancialAccountingDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(FinancialAccountingDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(FinancialAccountingData.LoadTrialBalanceByCompanyDate),
			(int CompanyId, DateTime StartDate, DateTime EndDate) => FinancialAccountingData.LoadTrialBalanceByCompanyDate(CompanyId, StartDate, EndDate));

		group.MapPost(nameof(FinancialAccountingData.DeleteTransaction),
			(FinancialAccountingModel accounting) => FinancialAccountingData.DeleteTransaction(accounting));

		group.MapPost(nameof(FinancialAccountingData.RecoverTransaction),
			(FinancialAccountingModel accounting) => FinancialAccountingData.RecoverTransaction(accounting));

		group.MapPost(nameof(FinancialAccountingData.SaveTransaction),
			(FinancialAccountingSaveRequest request) => FinancialAccountingData.SaveTransaction(request.Accounting, request.Ledgers, request.Recover));

		group.MapPost(nameof(FinancialAccountingData.SaveBRSDates),
			(List<FinancialAccountingLedgerModel> changedLines, int userId, string platform) => FinancialAccountingData.SaveBRSDates(changedLines, userId, platform));
	}
}

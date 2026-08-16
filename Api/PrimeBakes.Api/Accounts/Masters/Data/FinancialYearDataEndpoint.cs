using PrimeBakes.Library.Accounts.Masters.Data;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Accounts.Masters.Data;

public class FinancialYearDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(FinancialYearDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(FinancialYearData.LoadFinancialYearByDateTime),
			(DateTime TransactionDateTime) => FinancialYearData.LoadFinancialYearByDateTime(TransactionDateTime));

		group.MapPost(nameof(FinancialYearData.ValidateFinancialYear),
			(DateTime TransactionDateTime) => FinancialYearData.ValidateFinancialYear(TransactionDateTime));

		group.MapGet(nameof(FinancialYearData.GetDateRange), FinancialYearData.GetDateRange);

		group.MapPost(nameof(FinancialYearData.DeleteTransaction), FinancialYearData.DeleteTransaction);
		group.MapPost(nameof(FinancialYearData.RecoverTransaction), FinancialYearData.RecoverTransaction);
		group.MapPost(nameof(FinancialYearData.SaveTransaction), FinancialYearData.SaveTransaction);
	}
}

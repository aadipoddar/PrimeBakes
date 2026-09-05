using PrimeBakes.Data.Common;
using PrimeBakes.Models.Common;

namespace PrimeBakes.Api.Common;

public class CommonEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(CommonEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint).CacheOutput(ApiCachePolicy.Instance);

		group.MapGet(nameof(CommonData.LoadTableData),
			(string TableName, bool useLocalDB) => CommonData.LoadTableData<object>(TableName, null, useLocalDB));

		group.MapGet(nameof(CommonData.LoadTableDataById),
			(string TableName, int Id) => CommonData.LoadTableDataById<object>(TableName, Id));

		group.MapGet(nameof(CommonData.LoadTableDataByStatus),
			(string TableName, bool Status, bool useLocalDB) => CommonData.LoadTableDataByStatus<object>(TableName, Status, null, useLocalDB));

		group.MapGet(nameof(CommonData.LoadTableDataByMasterId),
			(string TableName, int MasterId) => CommonData.LoadTableDataByMasterId<object>(TableName, MasterId));

		group.MapGet(nameof(CommonData.LoadTableDataByFinancialAccountingId),
			(string TableName, int? FinancialAccountingId) => CommonData.LoadTableDataByFinancialAccountingId<object>(TableName, FinancialAccountingId));

		group.MapGet(nameof(CommonData.LoadTableDataByCode),
			(string TableName, string Code) => CommonData.LoadTableDataByCode<object>(TableName, Code));

		group.MapGet(nameof(CommonData.LoadTableDataByTransactionNo),
			(string TableName, string TransactionNo) => CommonData.LoadTableDataByTransactionNo<object>(TableName, TransactionNo));

		group.MapGet(nameof(CommonData.LoadTableDataByDate),
			(string TableName, DateTime StartDate, DateTime EndDate, bool useLocalDB) => CommonData.LoadTableDataByDate<object>(TableName, StartDate, EndDate, null, useLocalDB));

		group.MapGet(nameof(CommonData.LoadLastTableDataByFinancialYear),
			(string TableName, int FinancialYearId) => CommonData.LoadLastTableDataByFinancialYear<object>(TableName, FinancialYearId));

		group.MapGet(nameof(CommonData.LoadLastTableDataByLocationFinancialYear),
			(string TableName, int LocationId, int FinancialYearId) => CommonData.LoadLastTableDataByLocationFinancialYear<object>(TableName, LocationId, FinancialYearId));

		group.MapGet(nameof(CommonData.LoadCurrentDateTime),
			() => CommonData.LoadCurrentDateTime());

		group.MapGet(nameof(CommonData.LoadDatabaseLoad),
			() => CommonData.LoadDatabaseLoad());
	}
}

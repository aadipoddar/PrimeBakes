using PrimeBakes.Models.Common;

namespace PrimeBakes.Library.Common;

public static class CommonData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(CommonData));

	public static async Task<List<T>> LoadTableData<T>(string TableName) where T : new() =>
		await ApiClient.Get<List<T>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadTableData)), new { TableName });

	public static async Task<T> LoadTableDataById<T>(string TableName, int Id) where T : new() =>
		await ApiClient.Get<T>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadTableDataById)), new { TableName, Id });

	public static async Task<List<T>> LoadTableDataByStatus<T>(string TableName, bool Status = true) where T : new() =>
		await ApiClient.Get<List<T>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadTableDataByStatus)), new { TableName, Status });

	public static async Task<List<T>> LoadTableDataByMasterId<T>(string TableName, int MasterId) where T : new() =>
		await ApiClient.Get<List<T>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadTableDataByMasterId)), new { TableName, MasterId });

	public static async Task<List<T>> LoadTableDataByFinancialAccountingId<T>(string TableName, int? FinancialAccountingId) where T : new() =>
		await ApiClient.Get<List<T>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadTableDataByFinancialAccountingId)), new { TableName, FinancialAccountingId });

	public static async Task<T> LoadTableDataByCode<T>(string TableName, string Code) where T : new() =>
		await ApiClient.Get<T>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadTableDataByCode)), new { TableName, Code });

	public static async Task<T> LoadTableDataByTransactionNo<T>(string TableName, string TransactionNo) where T : new() =>
		await ApiClient.Get<T>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadTableDataByTransactionNo)), new { TableName, TransactionNo });

	public static async Task<List<T>> LoadTableDataByDate<T>(string TableName, DateTime StartDate, DateTime EndDate) where T : new() =>
		await ApiClient.Get<List<T>>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadTableDataByDate)), new { TableName, StartDate, EndDate });

	public static async Task<T> LoadLastTableData<T>(string TableName) where T : new() =>
		await ApiClient.Get<T>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadLastTableData)), new { TableName });

	public static async Task<T> LoadLastTableDataByFinancialYear<T>(string TableName, int FinancialYearId) where T : new() =>
		await ApiClient.Get<T>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadLastTableDataByFinancialYear)), new { TableName, FinancialYearId });

	public static async Task<T> LoadLastTableDataByCompanyFinancialYear<T>(string TableName, int CompanyId, int FinancialYearId) where T : new() =>
		await ApiClient.Get<T>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadLastTableDataByCompanyFinancialYear)), new { TableName, CompanyId, FinancialYearId });

	public static async Task<T> LoadLastTableDataByLocationFinancialYear<T>(string TableName, int LocationId, int FinancialYearId) where T : new() =>
		await ApiClient.Get<T>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadLastTableDataByLocationFinancialYear)), new { TableName, LocationId, FinancialYearId });

	public static async Task<DateTime> LoadCurrentDateTime() =>
		await ApiClient.Get<DateTime>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadCurrentDateTime)));
}

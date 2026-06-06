namespace PrimeBakesLibrary.Common;

public static class CommonData
{
	public static async Task<List<T>> LoadTableData<T>(string TableName, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadTableData, new { TableName }, sqlDataAccessTransaction);

	public static async Task<T> LoadTableDataById<T>(string TableName, int Id, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadTableDataById, new { TableName, Id }, sqlDataAccessTransaction)).FirstOrDefault();

	public static async Task<List<T>> LoadTableDataByStatus<T>(string TableName, bool Status = true, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadTableDataByStatus, new { TableName, Status }, sqlDataAccessTransaction);

	public static async Task<List<T>> LoadTableDataByMasterId<T>(string TableName, int MasterId, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadTableDataByMasterId, new { TableName, MasterId }, sqlDataAccessTransaction);

	public static async Task<List<T>> LoadTableDataByFinancialAccountingId<T>(string TableName, int? FinancialAccountingId, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadTableDataByFinancialAccountingId, new { TableName, FinancialAccountingId }, sqlDataAccessTransaction);

	public static async Task<T> LoadTableDataByCode<T>(string TableName, string Code, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadTableDataByCode, new { TableName, Code }, sqlDataAccessTransaction)).FirstOrDefault();

	public static async Task<T> LoadTableDataByTransactionNo<T>(string TableName, string TransactionNo, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadTableDataByTransactionNo, new { TableName, TransactionNo }, sqlDataAccessTransaction)).FirstOrDefault();

	public static async Task<List<T>> LoadTableDataByDate<T>(string TableName, DateTime StartDate, DateTime EndDate, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadTableDataByDate, new { TableName, StartDate, EndDate }, sqlDataAccessTransaction);

	public static async Task<T> LoadLastTableData<T>(string TableName, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadLastTableData, new { TableName }, sqlDataAccessTransaction)).FirstOrDefault();

	public static async Task<T> LoadLastTableDataByFinancialYear<T>(string TableName, int FinancialYearId, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadLastTableDataByFinancialYear, new { TableName, FinancialYearId }, sqlDataAccessTransaction)).FirstOrDefault();

	public static async Task<T> LoadLastTableDataByCompanyFinancialYear<T>(string TableName, int CompanyId, int FinancialYearId, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadLastTableDataByCompanyFinancialYear, new { TableName, CompanyId, FinancialYearId }, sqlDataAccessTransaction)).FirstOrDefault();

	public static async Task<T> LoadLastTableDataByLocationFinancialYear<T>(string TableName, int LocationId, int FinancialYearId, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
		(await SqlDataAccess.LoadData<T, dynamic>(CommonNames.LoadLastTableDataByLocationFinancialYear, new { TableName, LocationId, FinancialYearId }, sqlDataAccessTransaction)).FirstOrDefault();

	public static async Task<DateTime> LoadCurrentDateTime(SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<DateTime, dynamic>(CommonNames.LoadCurrentDateTime, new { }, sqlDataAccessTransaction)).FirstOrDefault();
}

namespace PrimeBakesLibrary.Data;

public static class CommonData
{
	public static async Task<List<T>> LoadTableData<T>(string TableName) where T : new() =>
			await SqlDataAccess.LoadData<T, dynamic>(StoredProcedure.LoadTableData, new { TableName });

	public static async Task<T> LoadTableDataById<T>(string TableName, int Id) where T : new() =>
			(await SqlDataAccess.LoadData<T, dynamic>(StoredProcedure.LoadTableDataById, new { TableName, Id })).FirstOrDefault();

	public static async Task<List<T>> LoadTableDataByStatus<T>(string TableName, bool Status) where T : new() =>
			await SqlDataAccess.LoadData<T, dynamic>(StoredProcedure.LoadTableDataByStatus, new { TableName, Status });

	public static async Task<T> LoadTableDataByIdActive<T>(string TableName, int Id) where T : new() =>
			(await SqlDataAccess.LoadData<T, dynamic>(StoredProcedure.LoadTableDataByIdActive, new { TableName, Id })).FirstOrDefault();

	public static async Task<T> LoadTableDataByCode<T>(string TableName, string Code) where T : new() =>
			(await SqlDataAccess.LoadData<T, dynamic>(StoredProcedure.LoadTableDataByCode, new { TableName, Code })).FirstOrDefault();

	public static async Task<T> LoadTableDataByCodeActive<T>(string TableName, string Code) where T : new() =>
			(await SqlDataAccess.LoadData<T, dynamic>(StoredProcedure.LoadTableDataByCodeActive, new { TableName, Code })).FirstOrDefault();
}
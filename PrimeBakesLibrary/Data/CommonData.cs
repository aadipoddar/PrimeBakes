namespace PrimeBakesLibrary.Data;

public static class CommonData
{
	public static async Task<List<T>> LoadTableData<T>(string tableName) where T : new() =>
			await SqlDataAccess.LoadData<T, dynamic>("Load_TableData", new { TableName = tableName });

	public static async Task<List<T>> LoadTableDataById<T>(string tableName, int id) where T : new() =>
			await SqlDataAccess.LoadData<T, dynamic>("Load_TableData_By_Id", new { TableName = tableName, Id = id });

	public static async Task<IEnumerable<T>> LoadTableDataByStatus<T>(string tableName, bool status) where T : new() =>
			await SqlDataAccess.LoadData<T, dynamic>("Load_TableData_By_Status", new { TableName = tableName, Status = status });
}
using Dapper;

using Microsoft.Data.SqlClient;

using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Maintenance;

using System.Data;

namespace PrimeBakes.Data.Operations.Maintenance;

public static class MaintenanceData
{
	public static async Task RebuildIndexes()
	{
		using SqlConnection connection = new(SqlDataAccess._databaseConnection);
		await connection.ExecuteAsync(OperationNames.RebuildIndexes, commandType: CommandType.StoredProcedure, commandTimeout: 0);
	}

	public static async Task<DatabaseSizeModel> LoadDatabaseSize() =>
		(await SqlDataAccess.LoadData<DatabaseSizeModel, dynamic>(OperationNames.LoadDatabaseSize, new { })).FirstOrDefault();
}

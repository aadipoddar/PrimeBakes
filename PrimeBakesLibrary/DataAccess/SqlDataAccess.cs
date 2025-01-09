using System.Data;

using Dapper;

using Microsoft.Data.SqlClient;

namespace PrimeBakesLibrary.DataAccess;

static class SqlDataAccess
{
	private static string ConnectionString = $"Server=tcp:salasarfoods.database.windows.net,1433;Initial Catalog={Secrets.DatabaseName};Persist Security Info=False;User ID=aadisql;Password={Secrets.DatabasePassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;";
	//private static string ConnectionString = $"Data Source=AADILAPI;Initial Catalog=PrimeBakes;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

	public static async Task<List<T>> LoadData<T, U>(string storedProcedure, U parameters)
	{
		using IDbConnection connection = new SqlConnection(ConnectionString);

		List<T> rows = (await connection.QueryAsync<T>(storedProcedure, parameters,
			commandType: CommandType.StoredProcedure)).ToList();

		return rows;
	}

	public static async Task SaveData<T>(string storedProcedure, T parameters)
	{
		using IDbConnection connection = new SqlConnection(ConnectionString);

		await connection.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
	}
}

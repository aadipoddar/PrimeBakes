using Dapper;

using Microsoft.Data.SqlClient;

using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace PrimeBakesLibrary.DataAccess;

internal static class SqlDataAccess
{
    public static readonly string _databaseConnection = Secrets.AzureConnectionString;

    public static async Task<List<T>> LoadData<T, U>(string storedProcedure, U parameters, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        if (sqlDataAccessTransaction is not null)
            return [.. await sqlDataAccessTransaction.LoadDataTransaction<T, U>(storedProcedure, parameters)];

        using IDbConnection connection = new SqlConnection(_databaseConnection);
        return [.. await connection.QueryAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure)];
    }

    public static async Task SaveData<T>(string storedProcedure, T parameters, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        if (sqlDataAccessTransaction is not null)
        {
            await sqlDataAccessTransaction.SaveDataTransaction(storedProcedure, parameters);
            return;
        }

        using IDbConnection connection = new SqlConnection(_databaseConnection);
        await connection.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
    }

    public static async Task ExecuteProcedure(string storedProcedure, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        if (sqlDataAccessTransaction is not null)
        {
            await sqlDataAccessTransaction.ExecuteProcedureTransaction(storedProcedure);
            return;
        }

        using IDbConnection connection = new SqlConnection(_databaseConnection);
        await connection.ExecuteAsync(storedProcedure, commandType: CommandType.StoredProcedure);
    }
}

public class SqlDataAccessTransaction : IDisposable
{
    private IDbConnection _connection;
    private IDbTransaction _transaction;

    public void StartTransaction()
    {
        _connection = new SqlConnection(SqlDataAccess._databaseConnection);
        _connection.Open();
        _transaction = _connection.BeginTransaction();
    }

    public async Task<List<T>> LoadDataTransaction<T, U>(string storedProcedure, U parameters) =>
        [.. await _connection.QueryAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure, transaction: _transaction)];

    public async Task SaveDataTransaction<T>(string storedProcedure, T parameters) =>
        await _connection.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure, transaction: _transaction);

    public async Task ExecuteProcedureTransaction(string storedProcedure) =>
        await _connection.ExecuteAsync(storedProcedure, commandType: CommandType.StoredProcedure, transaction: _transaction);

    public void CommitTransaction()
    {
        _transaction?.Commit();
        _connection?.Close();
    }

    public void RollbackTransaction()
    {
        _transaction?.Rollback();
        _connection?.Close();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();

        GC.SuppressFinalize(this);
    }
}

public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => value is DateOnly dateOnly
            ? dateOnly
            : DateOnly.FromDateTime((DateTime)value);

    public override void SetValue([DisallowNull] IDbDataParameter parameter, DateOnly value)
    {
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        parameter.DbType = DbType.Date;
    }
}

public class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override TimeOnly Parse(object value) => value is TimeOnly timeOnly
            ? timeOnly
            : TimeOnly.FromTimeSpan((TimeSpan)value);

    public override void SetValue([DisallowNull] IDbDataParameter parameter, TimeOnly value)
    {
        parameter.Value = value.ToTimeSpan();
        parameter.DbType = DbType.Time;
    }
}
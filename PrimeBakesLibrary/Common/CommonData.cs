namespace PrimeBakesLibrary.Common;

public static class CommonData
{
    public static async Task<List<T>> LoadTableData<T>(string TableName, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
            await SqlDataAccess.LoadData<T, dynamic>(OperationNames.LoadTableData, new { TableName }, sqlDataAccessTransaction);

    public static async Task<T> LoadTableDataById<T>(string TableName, int Id, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
        (await SqlDataAccess.LoadData<T, dynamic>(OperationNames.LoadTableDataById, new { TableName, Id }, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<List<T>> LoadTableDataByStatus<T>(string TableName, bool Status = true, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
        await SqlDataAccess.LoadData<T, dynamic>(OperationNames.LoadTableDataByStatus, new { TableName, Status }, sqlDataAccessTransaction);

    public static async Task<List<T>> LoadTableDataByMasterId<T>(string TableName, int MasterId, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
        await SqlDataAccess.LoadData<T, dynamic>(OperationNames.LoadTableDataByMasterId, new { TableName, MasterId }, sqlDataAccessTransaction);

    public static async Task<T> LoadTableDataByCode<T>(string TableName, string Code, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
        (await SqlDataAccess.LoadData<T, dynamic>(OperationNames.LoadTableDataByCode, new { TableName, Code }, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<T> LoadTableDataByTransactionNo<T>(string TableName, string TransactionNo, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
        (await SqlDataAccess.LoadData<T, dynamic>(OperationNames.LoadTableDataByTransactionNo, new { TableName, TransactionNo }, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<List<T>> LoadTableDataByDate<T>(string TableName, DateTime StartDate, DateTime EndDate, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
        await SqlDataAccess.LoadData<T, dynamic>(OperationNames.LoadTableDataByDate, new { TableName, StartDate, EndDate }, sqlDataAccessTransaction);

    public static async Task<T> LoadLastTableDataByFinancialYear<T>(string TableName, int FinancialYearId, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
        (await SqlDataAccess.LoadData<T, dynamic>(OperationNames.LoadLastTableDataByFinancialYear, new { TableName, FinancialYearId }, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<T> LoadLastTableDataByLocationFinancialYear<T>(string TableName, int LocationId, int FinancialYearId, SqlDataAccessTransaction sqlDataAccessTransaction = null) where T : new() =>
        (await SqlDataAccess.LoadData<T, dynamic>(OperationNames.LoadLastTableDataByLocationFinancialYear, new { TableName, LocationId, FinancialYearId }, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<DateTime> LoadCurrentDateTime() =>
        (await SqlDataAccess.LoadData<DateTime, dynamic>(OperationNames.LoadCurrentDateTime, new { })).FirstOrDefault();
}

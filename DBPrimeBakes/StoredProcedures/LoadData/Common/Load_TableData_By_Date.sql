CREATE PROCEDURE [dbo].[Load_TableData_By_Date]
	@TableName varchar(50),
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	-- Normalize date range: strip time, use exclusive upper bound
	SET @StartDate = CAST(@StartDate AS DATE);
	SET @EndDate = DATEADD(DAY, 1, CAST(@EndDate AS DATE));

	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName) + ' WHERE TransactionDateTime >= @StartDate AND TransactionDateTime < @EndDate';
	EXEC sp_executesql @sql,
					N'@StartDate DATETIME, @EndDate DATETIME',
					@StartDate = @StartDate,
					@EndDate = @EndDate;
END
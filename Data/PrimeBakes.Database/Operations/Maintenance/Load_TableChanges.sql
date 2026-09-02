CREATE PROCEDURE [dbo].[Load_TableChanges]
	@TableName VARCHAR(50),
	@KeyColumn VARCHAR(50),
	@LastVersion BIGINT
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @SQL NVARCHAR(MAX);
	SET @SQL = N'SELECT CONVERT(NVARCHAR(MAX), [ct].' + QUOTENAME(@KeyColumn) + N') AS [KeyValue], [ct].[SYS_CHANGE_OPERATION] AS [Operation] FROM CHANGETABLE(CHANGES ' + QUOTENAME(@TableName) + N', @LastVersion) AS [ct];';
	EXEC [sys].[sp_executesql] @SQL, N'@LastVersion BIGINT', @LastVersion;
END

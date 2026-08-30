CREATE PROCEDURE [dbo].[Load_TableData_By_Keys]
	@TableName VARCHAR(50),
	@KeyColumn VARCHAR(50),
	@Keys NVARCHAR(MAX)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @KeyType NVARCHAR(100);

	SELECT @KeyType = [t].[name] + CASE WHEN [t].[name] IN ('varchar', 'nvarchar', 'char', 'nchar') THEN
			'(' + CASE WHEN [c].[max_length] = -1 THEN 'MAX' ELSE CAST([c].[max_length] / CASE WHEN [t].[name] IN ('nvarchar', 'nchar') THEN 2 ELSE 1 END AS NVARCHAR(10)) END + ')'
		ELSE '' END
	FROM [sys].[columns] AS [c]
	INNER JOIN [sys].[types] AS [t] ON [t].[user_type_id] = [c].[user_type_id]
	WHERE [c].[object_id] = OBJECT_ID(@TableName) AND [c].[name] = @KeyColumn;

	DECLARE @SQL NVARCHAR(MAX);
	SET @SQL = N'SELECT * FROM ' + QUOTENAME(@TableName) + N' WHERE ' + QUOTENAME(@KeyColumn) +
		N' IN (SELECT CONVERT(' + @KeyType + N', [value]) FROM STRING_SPLIT(@Keys, CHAR(31))) ORDER BY ' + QUOTENAME(@KeyColumn) + N';';
	EXEC [sys].[sp_executesql] @SQL, N'@Keys NVARCHAR(MAX)', @Keys;
END
CREATE PROCEDURE [dbo].[Load_TableHashes]
	@TableName VARCHAR(50),
	@KeyColumn VARCHAR(50)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @Columns NVARCHAR(MAX);
	DECLARE @SQL NVARCHAR(MAX);

	SELECT @Columns = STRING_AGG(CAST('ISNULL(CONVERT(NVARCHAR(MAX),' + QUOTENAME([c].[name]) +
		CASE WHEN [t].[name] IN ('date', 'time', 'datetime', 'datetime2', 'smalldatetime', 'datetimeoffset') THEN ',126' ELSE '' END +
		'),''<NULL>'')' AS NVARCHAR(MAX)), '+''|''+') WITHIN GROUP (ORDER BY [c].[column_id])
	FROM [sys].[columns] AS [c]
	INNER JOIN [sys].[types] AS [t] ON [t].[user_type_id] = [c].[user_type_id]
	WHERE [c].[object_id] = OBJECT_ID(@TableName) AND [c].[is_computed] = 0;

	SET @SQL = N'SELECT CONVERT(NVARCHAR(MAX), ' + QUOTENAME(@KeyColumn) + N') AS [KeyValue], HASHBYTES(''SHA2_256'', ' + @Columns + N') AS [Hash] FROM ' + QUOTENAME(@TableName) + N' ORDER BY ' + QUOTENAME(@KeyColumn) + N';';
	EXEC [sys].[sp_executesql] @SQL;
END
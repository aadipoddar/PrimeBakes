CREATE PROCEDURE [dbo].[Rebuild_Indexes]
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @Commands NVARCHAR(MAX);

	SELECT @Commands = STRING_AGG(CAST('ALTER INDEX ALL ON ' + QUOTENAME(SCHEMA_NAME([t].[schema_id])) + '.' + QUOTENAME([t].[name]) + ' REBUILD;' AS NVARCHAR(MAX)), CHAR(10))
	FROM [sys].[tables] AS [t]
	WHERE EXISTS (SELECT 1 FROM [sys].[indexes] AS [i] WHERE [i].[object_id] = [t].[object_id] AND [i].[index_id] > 0);

	EXEC [sys].[sp_executesql] @Commands;
END
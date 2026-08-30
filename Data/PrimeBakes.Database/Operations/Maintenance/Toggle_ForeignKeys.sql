CREATE PROCEDURE [dbo].[Toggle_ForeignKeys]
	@Enable BIT
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @Commands NVARCHAR(MAX);

	SELECT @Commands = STRING_AGG(CAST('ALTER TABLE ' + QUOTENAME(SCHEMA_NAME([t].[schema_id])) + '.' + QUOTENAME([t].[name]) +
		CASE WHEN @Enable = 1 THEN ' WITH CHECK CHECK CONSTRAINT ALL;' ELSE ' NOCHECK CONSTRAINT ALL;' END AS NVARCHAR(MAX)), CHAR(10))
	FROM [sys].[tables] AS [t]
	WHERE [t].[is_ms_shipped] = 0;

	EXEC [sys].[sp_executesql] @Commands;
END
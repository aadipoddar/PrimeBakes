DECLARE @Commands NVARCHAR(MAX);

SELECT @Commands = STRING_AGG(CAST('ALTER TABLE ' + QUOTENAME(SCHEMA_NAME([t].[schema_id])) + '.' + QUOTENAME([t].[name]) +
	' ENABLE CHANGE_TRACKING;' AS NVARCHAR(MAX)), CHAR(10))
FROM [sys].[tables] AS [t]
WHERE [t].[is_ms_shipped] = 0
	AND [t].[name] NOT LIKE '\_\_%' ESCAPE '\'
	AND [t].[name] <> 'SyncVersion'
	AND NOT EXISTS (SELECT 1 FROM [sys].[change_tracking_tables] AS [c] WHERE [c].[object_id] = [t].[object_id]);

IF @Commands IS NOT NULL
	EXEC [sys].[sp_executesql] @Commands;

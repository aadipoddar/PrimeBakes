CREATE PROCEDURE [dbo].[Load_TableNames]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT [t].[name] AS [TableName],
		(SELECT TOP 1 [c].[name]
		 FROM [sys].[indexes] AS [i]
		 INNER JOIN [sys].[index_columns] AS [ic] ON [ic].[object_id] = [i].[object_id] AND [ic].[index_id] = [i].[index_id] AND [ic].[is_included_column] = 0
		 INNER JOIN [sys].[columns] AS [c] ON [c].[object_id] = [i].[object_id] AND [c].[column_id] = [ic].[column_id]
		 WHERE [i].[object_id] = [t].[object_id]
			AND ([i].[is_primary_key] = 1 OR [i].[is_unique_constraint] = 1 OR [i].[is_unique] = 1)
			AND (SELECT COUNT(*) FROM [sys].[index_columns] WHERE [object_id] = [i].[object_id] AND [index_id] = [i].[index_id] AND [is_included_column] = 0) = 1
		 ORDER BY [i].[is_primary_key] DESC, [i].[is_unique_constraint] DESC, [i].[index_id]) AS [KeyColumn]
	FROM [sys].[tables] AS [t]
	WHERE [t].[is_ms_shipped] = 0
	ORDER BY [t].[name];
END
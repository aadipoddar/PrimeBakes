CREATE PROCEDURE [dbo].[Load_TableChangeVersions]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT [t].[name] AS [TableName],
		CHANGE_TRACKING_CURRENT_VERSION() AS [CurrentVersion],
		CHANGE_TRACKING_MIN_VALID_VERSION([t].[object_id]) AS [MinValidVersion]
	FROM [sys].[change_tracking_tables] AS [c]
	INNER JOIN [sys].[tables] AS [t] ON [t].[object_id] = [c].[object_id]
	ORDER BY [t].[name];
END

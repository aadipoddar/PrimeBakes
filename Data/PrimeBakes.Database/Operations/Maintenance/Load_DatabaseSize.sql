CREATE PROCEDURE [dbo].[Load_DatabaseSize]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT CAST(ISNULL((SELECT SUM([used_page_count]) FROM [sys].[dm_db_partition_stats]), 0) * 8.0 / 1024 AS DECIMAL(18, 2)) AS [UsedMB];
END

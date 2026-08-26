CREATE PROCEDURE [dbo].[Load_DatabaseLoad]
AS
BEGIN
	SET NOCOUNT ON;

	IF SERVERPROPERTY('EngineEdition') <> 5
		SELECT CAST(0 AS DECIMAL(5, 2)) AS LoadPercent;
	ELSE
		EXEC sp_executesql N'SELECT CAST(AVG(avg_cpu_percent) AS DECIMAL(5, 2)) AS LoadPercent FROM (SELECT TOP (4) avg_cpu_percent FROM sys.dm_db_resource_stats ORDER BY end_time DESC) AS r;';
END

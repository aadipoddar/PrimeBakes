CREATE PROCEDURE [dbo].[Load_LastTableData]
	@TableName varchar(50)
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	SET @sql = N'SELECT TOP 1 * FROM ' + QUOTENAME(@TableName) + ' ORDER BY Id DESC';
	EXEC sp_executesql @sql
END
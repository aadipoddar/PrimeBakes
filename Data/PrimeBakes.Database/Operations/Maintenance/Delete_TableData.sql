CREATE PROCEDURE [dbo].[Delete_TableData]
	@TableName VARCHAR(50)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @SQL NVARCHAR(MAX);
	SET @SQL = N'DELETE FROM ' + QUOTENAME(@TableName) + N';';
	EXEC [sys].[sp_executesql] @SQL;
END
CREATE PROCEDURE [dbo].[Load_TableData_By_Code]
	@TableName VARCHAR(50),
	@Code VARCHAR(100)
AS
BEGIN

	SET NOCOUNT ON;

	DECLARE @sql NVARCHAR(MAX);
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName) + ' WHERE Code = @Code';

	-- Execute the dynamically generated SQL statement with parameter
	EXEC sp_executesql @sql,
					N'@Code VARCHAR(100)',
					@Code = @Code; 

END
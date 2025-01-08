CREATE PROCEDURE Load_TableData
	@TableName VARCHAR(50)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @sql NVARCHAR(MAX);
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName);

	-- Execute the dynamically generated SQL statement
	EXEC sp_executesql @sql; 

END
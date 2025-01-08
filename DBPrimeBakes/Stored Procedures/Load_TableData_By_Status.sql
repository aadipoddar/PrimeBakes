CREATE PROCEDURE Load_TableData_By_Status
	@TableName VARCHAR(50),
	@Status BIT
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @sql NVARCHAR(MAX);
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName) + ' WHERE Status = @Status';

	-- Execute the dynamically generated SQL statement with parameter
	EXEC sp_executesql @sql,
					N'@Status BIT', 
					@Status = @Status; 

END
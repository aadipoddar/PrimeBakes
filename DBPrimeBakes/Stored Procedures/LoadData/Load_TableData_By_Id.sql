CREATE PROCEDURE Load_TableData_By_Id
	@TableName VARCHAR(50),
	@Id INT
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @sql NVARCHAR(MAX);
	SET @sql = N'SELECT * FROM ' + QUOTENAME(@TableName) + ' WHERE Id = @Id';

	-- Execute the dynamically generated SQL statement with parameter
	EXEC sp_executesql @sql,
					N'@Id INT', 
					@Id = @Id; 

END
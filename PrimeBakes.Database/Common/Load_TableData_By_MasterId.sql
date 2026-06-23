CREATE PROCEDURE [dbo].[Load_TableData_By_MasterId]
	@TableName varchar(50),
	@MasterId int
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX)
	IF COL_LENGTH(@TableName, 'Status') IS NOT NULL
		SET @SQL = N'SELECT * FROM ' + QUOTENAME(@TableName) + N' WHERE MasterId = @MasterId AND Status = 1';
	ELSE
		SET @SQL = N'SELECT * FROM ' + QUOTENAME(@TableName) + N' WHERE MasterId = @MasterId';
	EXEC sp_executesql @SQL,
					N'@MasterId int',
					@MasterId = @MasterId
END
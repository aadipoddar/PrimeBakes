CREATE PROCEDURE [dbo].[Load_TableData_By_FinancialAccountingId]
	@TableName varchar(50),
	@FinancialAccountingId int
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @SQL nvarchar(MAX) = N'SELECT * FROM ' + QUOTENAME(@TableName) + N' WHERE (@FinancialAccountingId IS NULL AND FinancialAccountingId IS NULL) OR FinancialAccountingId = @FinancialAccountingId';
	EXEC sp_executesql @SQL,
					N'@FinancialAccountingId int',
					@FinancialAccountingId = @FinancialAccountingId
END

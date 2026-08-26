CREATE PROCEDURE [dbo].[Load_Last_AuditTrail_By_Table_Record]
	@TableName VARCHAR(100),
	@RecordNo VARCHAR(500)
AS
BEGIN
	SELECT TOP 1 *
	FROM [dbo].[AuditTrail]
	WHERE [TableName] = @TableName
		AND [RecordNo] = @RecordNo
		AND [Action] <> 'Report'
	ORDER BY [Id] DESC;
END

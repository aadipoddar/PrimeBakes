CREATE PROCEDURE [dbo].[Load_Last_AuditTrail_By_Table_Record]
	@TableName VARCHAR(MAX),
	@RecordNo VARCHAR(MAX)
AS
BEGIN
	SELECT TOP 1 *
	FROM [dbo].[AuditTrail]
	WHERE [TableName] = @TableName
		AND [RecordNo] = @RecordNo
	ORDER BY [Id] DESC;
END

CREATE PROCEDURE [dbo].[Load_AuditTrail_By_RecordNo]
	@RecordNo VARCHAR(500)
AS
BEGIN
	SELECT *
	FROM [dbo].[AuditTrail]
	WHERE [RecordNo] = @RecordNo
END
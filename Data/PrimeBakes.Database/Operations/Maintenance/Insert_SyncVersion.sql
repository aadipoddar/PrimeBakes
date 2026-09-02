CREATE PROCEDURE [dbo].[Insert_SyncVersion]
	@TableName VARCHAR(50),
	@Version BIGINT
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE [SyncVersion]
	SET [Version] = @Version,
		[LastSyncedAt] = (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time'))
	WHERE [TableName] = @TableName;

	IF @@ROWCOUNT = 0
		INSERT INTO [SyncVersion] ([TableName], [Version])
		VALUES (@TableName, @Version);
END

CREATE PROCEDURE [dbo].[Insert_OfflineQueue]
	@Id INT OUTPUT,
	@TableName VARCHAR(50),
	@TransactionNo VARCHAR(100),
	@Payload VARCHAR(MAX)
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[OfflineQueue]
		(
			[TableName],
			[TransactionNo],
			[Payload]
		)
		VALUES
		(
			@TableName,
			@TransactionNo,
			@Payload
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[OfflineQueue]
		SET
			[TableName] = @TableName,
			[TransactionNo] = @TransactionNo,
			[Payload] = @Payload
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END;
CREATE PROCEDURE [dbo].[Insert_WebPushSubscription]
	@Id INT OUTPUT,
	@UserId INT,
	@Endpoint VARCHAR(500),
	@P256dh VARCHAR(200),
	@Auth VARCHAR(100),
	@TransactionDateTime DATETIME
AS
BEGIN
	SET @Id = NULL;

	SELECT @Id = [Id]
	FROM [dbo].[WebPushSubscription]
	WHERE [Endpoint] = @Endpoint;

	IF @Id IS NULL
	BEGIN
		INSERT INTO [dbo].[WebPushSubscription]
		(
			[UserId],
			[Endpoint],
			[P256dh],
			[Auth]
		)
		VALUES
		(
			@UserId,
			@Endpoint,
			@P256dh,
			@Auth
		);

		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[WebPushSubscription]
		SET [UserId] = @UserId,
			[P256dh] = @P256dh,
			[Auth] = @Auth
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END

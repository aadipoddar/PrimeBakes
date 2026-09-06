CREATE PROCEDURE [dbo].[Insert_Terminal]
	@Id INT OUTPUT,
	@TerminalNo INT,
	@MachineId VARCHAR(100),
	@MachineName VARCHAR(MAX),
	@UserId INT,
	@LastSyncedAt DATETIME,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Terminal]
		(
			[TerminalNo],
			[MachineId],
			[MachineName],
			[UserId],
			[LastSyncedAt],
			[Status]
		)
		VALUES
		(
			@TerminalNo,
			@MachineId,
			@MachineName,
			@UserId,
			@LastSyncedAt,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Terminal]
		SET
			[TerminalNo] = @TerminalNo,
			[MachineId] = @MachineId,
			[MachineName] = @MachineName,
			[UserId] = @UserId,
			[LastSyncedAt] = @LastSyncedAt,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END;
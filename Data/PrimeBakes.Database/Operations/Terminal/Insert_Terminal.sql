CREATE PROCEDURE [dbo].[Insert_Terminal]
	@Id INT OUTPUT,
	@TerminalNo INT,
	@MachineId VARCHAR(100),
	@MachineName VARCHAR(MAX),
	@UserId INT,
	@LastSyncedAt DATETIME
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
			[LastSyncedAt]
		)
		VALUES
		(
			@TerminalNo,
			@MachineId,
			@MachineName,
			@UserId,
			@LastSyncedAt
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
			[LastSyncedAt] = @LastSyncedAt
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END;
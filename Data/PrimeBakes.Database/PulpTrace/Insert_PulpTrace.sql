CREATE PROCEDURE [dbo].[Insert_PulpTrace]
	@Id INT OUTPUT, 
	@TransactionDateTime DATETIME,
	@MachineName VARCHAR(MAX),
	@ReelNo VARCHAR(MAX),
	@DeckleCM INT,
	@GSM INT,
	@DF INT,
	@PaperShade VARCHAR(MAX),
	@ActualWeight INT,
	@UsedWeight INT,
	@BalanceWeight INT,
	@Scanned BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[PulpTrace]
		(
			[TransactionDateTime],
			[MachineName],
			[ReelNo],
			[DeckleCM],
			[GSM],
			[DF],
			[PaperShade],
			[ActualWeight],
			[UsedWeight],
			[BalanceWeight],
			[Scanned]
		)
		VALUES
		(
			@TransactionDateTime,
			@MachineName,
			@ReelNo,
			@DeckleCM,
			@GSM,
			@DF,
			@PaperShade,
			@ActualWeight,
			@UsedWeight,
			@BalanceWeight,
			@Scanned
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[PulpTrace]
		SET
			[TransactionDateTime] = @TransactionDateTime,
			[MachineName] = @MachineName,
			[ReelNo] = @ReelNo,
			[DeckleCM] = @DeckleCM,
			[GSM] = @GSM,
			[DF] = @DF,
			[PaperShade] = @PaperShade,
			[ActualWeight] = @ActualWeight,
			[UsedWeight] = @UsedWeight,
			[BalanceWeight] = @BalanceWeight,
			[Scanned] = @Scanned
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END;
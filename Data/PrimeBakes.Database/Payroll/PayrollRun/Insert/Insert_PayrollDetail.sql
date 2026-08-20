CREATE PROCEDURE [dbo].[Insert_PayrollDetail]
	@Id INT OUTPUT,
	@MasterId INT,
	@SalaryComponentId INT,
	@Amount MONEY,
	@Formula VARCHAR(MAX),
	@Prorate BIT,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[PayrollDetail]
		(
			[MasterId],
			[SalaryComponentId],
			[Amount],
			[Formula],
			[Prorate],
			[Status]
		)
		VALUES
		(
			@MasterId,
			@SalaryComponentId,
			@Amount,
			@Formula,
			@Prorate,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[PayrollDetail]
		SET [MasterId] = @MasterId,
			[SalaryComponentId] = @SalaryComponentId,
			[Amount] = @Amount,
			[Formula] = @Formula,
			[Prorate] = @Prorate,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS 'Id';
END

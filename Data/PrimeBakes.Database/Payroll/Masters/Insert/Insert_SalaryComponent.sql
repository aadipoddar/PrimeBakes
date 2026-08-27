CREATE PROCEDURE [dbo].[Insert_SalaryComponent]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@Code VARCHAR(50),
	@ComponentType VARCHAR(30),
	@Formula VARCHAR(MAX) = NULL,
	@Sequence INT,
	@Prorate BIT,
	@Rounding BIT,
	@ShowOnPayslip BIT,
	@Remarks VARCHAR(MAX) = NULL,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[SalaryComponent]
		(
			[Name],
			[Code],
			[ComponentType],
			[Formula],
			[Sequence],
			[Prorate],
			[Rounding],
			[ShowOnPayslip],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Code,
			@ComponentType,
			@Formula,
			@Sequence,
			@Prorate,
			@Rounding,
			@ShowOnPayslip,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[SalaryComponent]
		SET [Name] = @Name,
			[Code] = @Code,
			[ComponentType] = @ComponentType,
			[Formula] = @Formula,
			[Sequence] = @Sequence,
			[Prorate] = @Prorate,
			[Rounding] = @Rounding,
			[ShowOnPayslip] = @ShowOnPayslip,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END;

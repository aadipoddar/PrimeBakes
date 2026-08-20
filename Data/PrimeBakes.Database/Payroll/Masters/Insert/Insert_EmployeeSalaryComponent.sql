CREATE PROCEDURE [dbo].[Insert_EmployeeSalaryComponent]
	@Id INT OUTPUT,
	@EmployeeId INT,
	@SalaryComponentId INT,
	@Amount MONEY,
	@Formula VARCHAR(MAX),
	@Prorate BIT,
	@FromDate DATE,
	@Remarks VARCHAR(MAX)
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[EmployeeSalaryComponent]
		(
			[EmployeeId],
			[SalaryComponentId],
			[Amount],
			[Formula],
			[Prorate],
			[FromDate],
			[Remarks]
		)
		VALUES
		(
			@EmployeeId,
			@SalaryComponentId,
			@Amount,
			@Formula,
			@Prorate,
			@FromDate,
			@Remarks
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[EmployeeSalaryComponent]
		SET [EmployeeId] = @EmployeeId,
			[SalaryComponentId] = @SalaryComponentId,
			[Amount] = @Amount,
			[Formula] = @Formula,
			[Prorate] = @Prorate,
			[FromDate] = @FromDate,
			[Remarks] = @Remarks
		WHERE [Id] = @Id;
	END

	SELECT @Id AS 'Id';
END

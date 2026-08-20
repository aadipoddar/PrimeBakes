CREATE PROCEDURE [dbo].[Insert_Payroll]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(100),
	@EmployeeId INT,
	@PayrollMonth INT,
	@PayrollYear INT,
	@TransactionDateTime DATETIME,
	@FinancialYearId INT,
	@AttendanceId INT,
	@DaysInMonth DECIMAL(5, 2),
	@PaidDays DECIMAL(5, 2),
	@GrossEarnings MONEY,
	@TotalDeductions MONEY,
	@EmployerContribution MONEY,
	@NetPay MONEY,
	@Remarks VARCHAR(MAX),
	@CreatedBy INT,
	@CreatedAt DATETIME,
	@CreatedFromPlatform VARCHAR(MAX),
	@Status BIT,
	@LastModifiedBy INT,
	@LastModifiedAt DATETIME,
	@LastModifiedFromPlatform VARCHAR(MAX)
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Payroll]
		(
			[TransactionNo],
			[EmployeeId],
			[PayrollMonth],
			[PayrollYear],
			[TransactionDateTime],
			[FinancialYearId],
			[AttendanceId],
			[DaysInMonth],
			[PaidDays],
			[GrossEarnings],
			[TotalDeductions],
			[EmployerContribution],
			[NetPay],
			[Remarks],
			[CreatedBy],
			[CreatedAt],
			[CreatedFromPlatform],
			[Status],
			[LastModifiedBy],
			[LastModifiedAt],
			[LastModifiedFromPlatform]
		)
		VALUES
		(
			@TransactionNo,
			@EmployeeId,
			@PayrollMonth,
			@PayrollYear,
			@TransactionDateTime,
			@FinancialYearId,
			@AttendanceId,
			@DaysInMonth,
			@PaidDays,
			@GrossEarnings,
			@TotalDeductions,
			@EmployerContribution,
			@NetPay,
			@Remarks,
			@CreatedBy,
			@CreatedAt,
			@CreatedFromPlatform,
			@Status,
			@LastModifiedBy,
			@LastModifiedAt,
			@LastModifiedFromPlatform
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Payroll]
		SET [TransactionNo] = @TransactionNo,
			[EmployeeId] = @EmployeeId,
			[PayrollMonth] = @PayrollMonth,
			[PayrollYear] = @PayrollYear,
			[TransactionDateTime] = @TransactionDateTime,
			[FinancialYearId] = @FinancialYearId,
			[AttendanceId] = @AttendanceId,
			[DaysInMonth] = @DaysInMonth,
			[PaidDays] = @PaidDays,
			[GrossEarnings] = @GrossEarnings,
			[TotalDeductions] = @TotalDeductions,
			[EmployerContribution] = @EmployerContribution,
			[NetPay] = @NetPay,
			[Remarks] = @Remarks,
			[CreatedBy] = @CreatedBy,
			[CreatedAt] = @CreatedAt,
			[CreatedFromPlatform] = @CreatedFromPlatform,
			[Status] = @Status,
			[LastModifiedBy] = @LastModifiedBy,
			[LastModifiedAt] = @LastModifiedAt,
			[LastModifiedFromPlatform] = @LastModifiedFromPlatform
		WHERE [Id] = @Id;
	END

	SELECT @Id AS 'Id';
END

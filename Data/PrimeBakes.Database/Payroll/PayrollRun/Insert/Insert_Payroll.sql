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
	@CreatedFormFactor VARCHAR(MAX),
	@CreatedPlatform VARCHAR(MAX),
	@CreatedLatitude DECIMAL(9,6),
	@CreatedLongitude DECIMAL(9,6),
	@Status BIT,
	@LastModifiedBy INT,
	@LastModifiedAt DATETIME,
	@LastModifiedFormFactor VARCHAR(MAX),
	@LastModifiedPlatform VARCHAR(MAX),
	@LastModifiedLatitude DECIMAL(9,6),
	@LastModifiedLongitude DECIMAL(9,6)
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
			[CreatedFormFactor],
			[CreatedPlatform],
			[CreatedLatitude],
			[CreatedLongitude],
			[Status],
			[LastModifiedBy],
			[LastModifiedAt],
			[LastModifiedFormFactor],
			[LastModifiedPlatform],
			[LastModifiedLatitude],
			[LastModifiedLongitude]
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
			@CreatedFormFactor,
			@CreatedPlatform,
			@CreatedLatitude,
			@CreatedLongitude,
			@Status,
			@LastModifiedBy,
			@LastModifiedAt,
			@LastModifiedFormFactor,
			@LastModifiedPlatform,
			@LastModifiedLatitude,
			@LastModifiedLongitude
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
			[CreatedFormFactor] = @CreatedFormFactor,
			[CreatedPlatform] = @CreatedPlatform,
			[CreatedLatitude] = @CreatedLatitude,
			[CreatedLongitude] = @CreatedLongitude,
			[Status] = @Status,
			[LastModifiedBy] = @LastModifiedBy,
			[LastModifiedAt] = @LastModifiedAt,
			[LastModifiedFormFactor] = @LastModifiedFormFactor,
			[LastModifiedPlatform] = @LastModifiedPlatform,
			[LastModifiedLatitude] = @LastModifiedLatitude,
			[LastModifiedLongitude] = @LastModifiedLongitude
		WHERE [Id] = @Id;
	END

	SELECT @Id AS 'Id';
END

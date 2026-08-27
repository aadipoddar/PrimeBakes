CREATE PROCEDURE [dbo].[Insert_FinancialAccounting]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(100),
	@CompanyId INT,
	@VoucherId INT,
	@ReferenceId INT,
	@ReferenceNo VARCHAR(MAX),
	@TransactionDateTime DATETIME,
	@FinancialYearId INT,
	@TotalDebitLedgers INT,
	@TotalCreditLedgers INT,
	@TotalDebitAmount MONEY,
	@TotalCreditAmount MONEY,
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
		INSERT INTO [dbo].[FinancialAccounting]
		(
			[TransactionNo],
			[CompanyId],
			[VoucherId],
			[ReferenceId],
			[ReferenceNo],
			[TransactionDateTime],
			[FinancialYearId],
			[TotalDebitLedgers],
			[TotalCreditLedgers],
			[TotalDebitAmount],
			[TotalCreditAmount],
			[Remarks],
			[CreatedBy],
			[CreatedAt],
			[CreatedFormFactor],
			[CreatedPlatform],
			[CreatedLatitude],
			[CreatedLongitude],
			[Status]
		) VALUES
		(
			@TransactionNo,
			@CompanyId,
			@VoucherId,
			@ReferenceId,
			@ReferenceNo,
			@TransactionDateTime,
			@FinancialYearId,
			@TotalDebitLedgers,
			@TotalCreditLedgers,
			@TotalDebitAmount,
			@TotalCreditAmount,
			@Remarks,
			@CreatedBy,
			@CreatedAt,
			@CreatedFormFactor,
			@CreatedPlatform,
			@CreatedLatitude,
			@CreatedLongitude,
			@Status
		)

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[FinancialAccounting]
		SET
			[TransactionNo] = @TransactionNo,
			[CompanyId] = @CompanyId,
			[VoucherId] = @VoucherId,
			[ReferenceId] = @ReferenceId,
			[ReferenceNo] = @ReferenceNo,
			[TransactionDateTime] = @TransactionDateTime,
			[FinancialYearId] = @FinancialYearId,
			[TotalDebitLedgers] = @TotalDebitLedgers,
			[TotalCreditLedgers] = @TotalCreditLedgers,
			[TotalDebitAmount] = @TotalDebitAmount,
			[TotalCreditAmount] = @TotalCreditAmount,
			[Remarks] = @Remarks,
			[Status] = @Status,
			[LastModifiedBy] = @LastModifiedBy,
			[LastModifiedAt] = @LastModifiedAt,
			[LastModifiedFormFactor] = @LastModifiedFormFactor,
			[LastModifiedPlatform] = @LastModifiedPlatform,
			[LastModifiedLatitude] = @LastModifiedLatitude,
			[LastModifiedLongitude] = @LastModifiedLongitude
		WHERE
			[Id] = @Id
	END

	SELECT @Id AS Id
END
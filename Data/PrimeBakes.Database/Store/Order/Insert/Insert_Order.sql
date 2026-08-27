CREATE PROCEDURE [dbo].[Insert_Order]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(100),

	@CompanyId INT,
	@LocationId INT,
	@SaleId INT = NULL,
	@FinancialYearId INT,

	@TransactionDateTime DATETIME,
	@TotalItems INT,
	@TotalQuantity MONEY,
	@Remarks VARCHAR(MAX),

	@Status BIT,

	@CreatedBy INT,
	@CreatedAt DATETIME,
	@CreatedFormFactor VARCHAR(MAX),
	@CreatedPlatform VARCHAR(MAX),
	@CreatedLatitude DECIMAL(9,6),
	@CreatedLongitude DECIMAL(9,6),

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
		INSERT INTO [dbo].[Order]
		(
			[TransactionNo],
			[CompanyId],
			[LocationId],
			[SaleId],
			[FinancialYearId],
			[TransactionDateTime],
			[TotalItems],
			[TotalQuantity],
			[Remarks],
			[Status],
			[CreatedBy],
			[CreatedAt],
			[CreatedFormFactor],
			[CreatedPlatform],
			[CreatedLatitude],
			[CreatedLongitude]
		)
		VALUES
		(
			@TransactionNo,
			@CompanyId,
			@LocationId,
			@SaleId,
			@FinancialYearId,
			@TransactionDateTime,
			@TotalItems,
			@TotalQuantity,
			@Remarks,
			@Status,
			@CreatedBy,
			@CreatedAt,
			@CreatedFormFactor,
			@CreatedPlatform,
			@CreatedLatitude,
			@CreatedLongitude
		)
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Order]
		SET
			[TransactionNo] = @TransactionNo,
			[CompanyId] = @CompanyId,
			[LocationId] = @LocationId,
			[SaleId] = @SaleId,
			[FinancialYearId] = @FinancialYearId,
			[TransactionDateTime] = @TransactionDateTime,
			[TotalItems] = @TotalItems,
			[TotalQuantity] = @TotalQuantity,
			[Remarks] = @Remarks,
			[Status] = @Status,
			[LastModifiedBy] = @LastModifiedBy,
			[LastModifiedAt] = @LastModifiedAt,
			[LastModifiedFormFactor] = @LastModifiedFormFactor,
			[LastModifiedPlatform] = @LastModifiedPlatform,
			[LastModifiedLatitude] = @LastModifiedLatitude,
			[LastModifiedLongitude] = @LastModifiedLongitude
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END
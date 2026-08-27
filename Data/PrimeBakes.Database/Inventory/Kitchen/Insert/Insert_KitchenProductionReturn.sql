CREATE PROCEDURE [dbo].[Insert_KitchenProductionReturn]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(100),
	@CompanyId INT,
	@TransactionDateTime DATETIME,
	@FinancialYearId INT,
	@KitchenId INT,
	@TotalItems INT,
	@TotalQuantity MONEY,
	@TotalAmount MONEY,
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
		INSERT INTO [dbo].[KitchenProductionReturn]
		(
			[TransactionNo],
			[CompanyId],
			[TransactionDateTime],
			[FinancialYearId],
			[KitchenId],
			[TotalItems],
			[TotalQuantity],
			[TotalAmount],
			[Remarks],
			[CreatedBy],
			[CreatedAt],
			[CreatedFormFactor],
			[CreatedPlatform],
			[CreatedLatitude],
			[CreatedLongitude],
			[Status]
		)
		VALUES
		(
			@TransactionNo,
			@CompanyId,
			@TransactionDateTime,
			@FinancialYearId,
			@KitchenId,
			@TotalItems,
			@TotalQuantity,
			@TotalAmount,
			@Remarks,
			@CreatedBy,
			@CreatedAt,
			@CreatedFormFactor,
			@CreatedPlatform,
			@CreatedLatitude,
			@CreatedLongitude,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[KitchenProductionReturn]
		SET
			[CompanyId] = @CompanyId,
			[TransactionDateTime] = @TransactionDateTime,
			[FinancialYearId] = @FinancialYearId,
			[KitchenId] = @KitchenId,
			[TotalItems] = @TotalItems,
			[TotalQuantity] = @TotalQuantity,
			[TotalAmount] = @TotalAmount,
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
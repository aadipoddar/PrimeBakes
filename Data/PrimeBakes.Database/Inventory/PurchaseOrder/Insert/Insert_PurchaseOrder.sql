CREATE PROCEDURE [dbo].[Insert_PurchaseOrder]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(100),
	@CompanyId INT,
	@PartyId INT,
	@PurchaseId INT = NULL,
	@TransactionDateTime DATETIME,
	@ExpectedDeliveryDate DATE = NULL,
	@FinancialYearId INT,
	@TotalItems INT,
	@TotalQuantity MONEY,
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
		INSERT INTO [dbo].[PurchaseOrder]
		(
			[TransactionNo],
			[CompanyId],
			[PartyId],
			[PurchaseId],
			[TransactionDateTime],
			[ExpectedDeliveryDate],
			[FinancialYearId],
			[TotalItems],
			[TotalQuantity],
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
			@PartyId,
			@PurchaseId,
			@TransactionDateTime,
			@ExpectedDeliveryDate,
			@FinancialYearId,
			@TotalItems,
			@TotalQuantity,
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
		UPDATE [dbo].[PurchaseOrder]
		SET
			[TransactionNo] = @TransactionNo,
			[CompanyId] = @CompanyId,
			[PartyId] = @PartyId,
			[PurchaseId] = @PurchaseId,
			[TransactionDateTime] = @TransactionDateTime,
			[ExpectedDeliveryDate] = @ExpectedDeliveryDate,
			[FinancialYearId] = @FinancialYearId,
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
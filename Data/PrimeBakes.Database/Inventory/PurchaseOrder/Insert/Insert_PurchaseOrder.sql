CREATE PROCEDURE [dbo].[Insert_PurchaseOrder]
	@Id INT OUTPUT,
	@TransactionNo VARCHAR(MAX),
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
	@CreatedFromPlatform VARCHAR(MAX),
	@Status BIT,
	@LastModifiedBy INT,
	@LastModifiedAt DATETIME,
	@LastModifiedFromPlatform VARCHAR(MAX)
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
			[CreatedFromPlatform],
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
			@CreatedFromPlatform,
			@Status
		)
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[PurchaseOrder]
		SET
			TransactionNo = @TransactionNo,
			CompanyId = @CompanyId,
			PartyId = @PartyId,
			PurchaseId = @PurchaseId,
			TransactionDateTime = @TransactionDateTime,
			ExpectedDeliveryDate = @ExpectedDeliveryDate,
			FinancialYearId = @FinancialYearId,
			TotalItems = @TotalItems,
			TotalQuantity = @TotalQuantity,
			Remarks = @Remarks,
			Status = @Status,
			LastModifiedBy = @LastModifiedBy,
			LastModifiedAt = @LastModifiedAt,
			LastModifiedFromPlatform = @LastModifiedFromPlatform
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END
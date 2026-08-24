CREATE PROCEDURE [dbo].[Insert_PurchaseOrderDetail]
	@Id INT OUTPUT,
	@MasterId INT,
	@RawMaterialId INT,
	@Quantity MONEY,
	@UnitOfMeasurement VARCHAR(20),
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[PurchaseOrderDetail]
		(
			[MasterId],
			RawMaterialId,
			Quantity,
			UnitOfMeasurement,
			Remarks,
			Status
		) VALUES
		(
			@MasterId,
			@RawMaterialId,
			@Quantity,
			@UnitOfMeasurement,
			@Remarks,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[PurchaseOrderDetail]
		SET
			[MasterId] = @MasterId,
			RawMaterialId = @RawMaterialId,
			Quantity = @Quantity,
			UnitOfMeasurement = @UnitOfMeasurement,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;
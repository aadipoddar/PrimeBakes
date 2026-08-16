CREATE PROCEDURE [dbo].[Insert_Location]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Code VARCHAR(10),
	@Discount DECIMAL(5, 2),
	@LedgerId INT,
	@COCO BIT,
	@FOFO BIT,
	@UseLocationRateOnSale BIT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Location]
		(
			[Name],
			[Code],
			[Discount],
			[LedgerId],
			[COCO],
			[FOFO],
			[UseLocationRateOnSale],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Code,
			@Discount,
			@LedgerId,
			@COCO,
			@FOFO,
			@UseLocationRateOnSale,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Location]
		SET
			[Name] = @Name,
			[Code] = @Code,
			[Discount] = @Discount,
			[LedgerId] = @LedgerId,
			[COCO] = @COCO,
			[FOFO] = @FOFO,
			[UseLocationRateOnSale] = @UseLocationRateOnSale,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;
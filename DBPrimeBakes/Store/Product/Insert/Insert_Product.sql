CREATE PROCEDURE [dbo].[Insert_Product]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Code VARCHAR(10),
	@ProductCategoryId INT,
	@KOTCategoryId INT,
	@Rate MONEY,
	@TaxId INT,
	@FoodType VARCHAR(100),
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Product]
		(
			[ProductCategoryId],
			[KOTCategoryId], 
			[Code],
			[Name],
			[Rate],
			[TaxId],
			[FoodType],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@ProductCategoryId,
			@KOTCategoryId,
			@Code,
			@Name,
			@Rate, 
			@TaxId, 
			@FoodType,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Product]
		SET [ProductCategoryId] = @ProductCategoryId, 
			[KOTCategoryId] = @KOTCategoryId,
			[Code] = @Code, 
			[Name] = @Name, 
			[Rate] = @Rate, 
			[TaxId] = @TaxId, 
			[FoodType] = @FoodType,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END
	SELECT @Id AS Id;
END
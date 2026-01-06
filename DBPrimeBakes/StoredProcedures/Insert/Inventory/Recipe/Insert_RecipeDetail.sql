CREATE PROCEDURE [dbo].[Insert_RecipeDetail]
	@Id INT OUTPUT,
	@MasterId INT,
	@RawMaterialId INT,
	@Quantity MONEY,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[RecipeDetail] ([MasterId], [RawMaterialId], [Quantity], [Status])
		VALUES (@MasterId, @RawMaterialId, @Quantity, @Status);

		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[RecipeDetail]
		SET [MasterId] = @MasterId, 
			[RawMaterialId] = @RawMaterialId, 
			[Quantity] = @Quantity, 
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END
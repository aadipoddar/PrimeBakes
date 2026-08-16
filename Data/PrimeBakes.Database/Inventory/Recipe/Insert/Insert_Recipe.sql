CREATE PROCEDURE [dbo].[Insert_Recipe]
	@Id INT OUTPUT,
	@ProductId INT,
	@Quantity MONEY,
	@Deduct BIT,
	@FromDate DATE,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Recipe] ([ProductId], [Quantity], [Deduct], [FromDate], [Status])
		VALUES (@ProductId, @Quantity, @Deduct, @FromDate, @Status);

		SET @Id = SCOPE_IDENTITY();
	END
	ELSE
	BEGIN
		UPDATE [dbo].[Recipe]
		SET [ProductId] = @ProductId,
			[Quantity] = @Quantity,
			[Deduct] = @Deduct,
			[FromDate] = @FromDate,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END
CREATE PROCEDURE [dbo].[Insert_ProductStock]
	@Id INT,
	@ProductId INT, 
	@Quantity MONEY, 
	@NetRate MONEY,
	@Type VARCHAR(20), 
	@TransactionId INT,
	@TransactionNo VARCHAR(MAX),
	@TransactionDateTime DATETIME,
	@LocationId INT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[ProductStock] 
		(
			[ProductId], 
			[Quantity], 
			[NetRate],
			[Type], 
			[TransactionId],
			[TransactionNo],
			[TransactionDateTime], 
			[LocationId]
		)
		VALUES
		(
			@ProductId, 
			@Quantity,
			@NetRate,
			@Type, 
			@TransactionId,
			@TransactionNo,
			@TransactionDateTime, 
			@LocationId
		);

		SET @Id = SCOPE_IDENTITY();
	END
	ELSE

	BEGIN
		UPDATE [dbo].[ProductStock]
		SET 
			[ProductId] = @ProductId, 
			[Quantity] = @Quantity, 
			[NetRate] = @NetRate,
			[Type] = @Type, 
			[TransactionId] = @TransactionId,
			[TransactionNo] = @TransactionNo,
			[TransactionDateTime] = @TransactionDateTime, 
			[LocationId] = @LocationId
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END;
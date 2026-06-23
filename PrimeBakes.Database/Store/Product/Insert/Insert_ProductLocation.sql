CREATE PROCEDURE [dbo].[Insert_ProductLocation]
	@Id INT OUTPUT,
	@ProductId INT,
	@Rate MONEY,
	@LocationId INT,
	@FromDate DATE
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[ProductLocation]
		(
			[ProductId],
			[Rate], 
			[LocationId],
			[FromDate]
		)
		VALUES
		(
			@ProductId, 
			@Rate, 
			@LocationId,
			@FromDate
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[ProductLocation]
		SET [ProductId] = @ProductId, 
			[Rate] = @Rate, 
			[LocationId] = @LocationId,
			[FromDate] = @FromDate
		WHERE [Id] = @Id;
	END

	SELECT @Id AS 'Id';
END
CREATE PROCEDURE [dbo].[Insert_ProductStock_List]
	@ProductStocks [dbo].[ProductStockType] READONLY
AS
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
	SELECT
		[ProductId],
		[Quantity],
		[NetRate],
		[Type],
		[TransactionId],
		[TransactionNo],
		[TransactionDateTime],
		[LocationId]
	FROM @ProductStocks;
END;
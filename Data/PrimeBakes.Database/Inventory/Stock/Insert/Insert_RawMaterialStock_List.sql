CREATE PROCEDURE [dbo].[Insert_RawMaterialStock_List]
	@RawMaterialStocks [dbo].[RawMaterialStockType] READONLY
AS
BEGIN
	INSERT INTO [dbo].[RawMaterialStock]
	(
		[RawMaterialId],
		[Quantity],
		[NetRate],
		[Type],
		[TransactionId],
		[TransactionNo],
		[TransactionDateTime]
	)
	SELECT
		[RawMaterialId],
		[Quantity],
		[NetRate],
		[Type],
		[TransactionId],
		[TransactionNo],
		[TransactionDateTime]
	FROM @RawMaterialStocks;
END;
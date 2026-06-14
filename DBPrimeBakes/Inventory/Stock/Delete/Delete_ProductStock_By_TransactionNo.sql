CREATE PROCEDURE [dbo].[Delete_ProductStock_By_TransactionNo]
	@TransactionNo VARCHAR(100)
AS
BEGIN
	DELETE FROM [dbo].[ProductStock]
	WHERE [TransactionNo] = @TransactionNo;

	SELECT 1 AS Success;
END
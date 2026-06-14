CREATE PROCEDURE [dbo].[Delete_RawMaterialStock_By_TransactionNo]
	@TransactionNo VARCHAR(100)
AS
BEGIN
	DELETE FROM [dbo].[RawMaterialStock]
	WHERE [TransactionNo] = @TransactionNo;

	SELECT 1 AS Success;
END
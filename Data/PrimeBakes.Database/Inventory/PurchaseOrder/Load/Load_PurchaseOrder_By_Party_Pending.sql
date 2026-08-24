CREATE PROCEDURE [dbo].[Load_PurchaseOrder_By_Party_Pending]
	@PartyId INT
AS
BEGIN
	SELECT
		*
	FROM [dbo].[PurchaseOrder] po
	WHERE po.PartyId = @PartyId
		AND po.PurchaseId IS NULL
		AND po.Status = 1
END
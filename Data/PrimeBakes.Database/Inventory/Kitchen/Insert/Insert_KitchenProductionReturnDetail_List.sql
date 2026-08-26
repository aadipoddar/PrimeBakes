CREATE PROCEDURE [dbo].[Insert_KitchenProductionReturnDetail_List]
	@KitchenProductionReturnDetails [dbo].[KitchenProductionReturnDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[KitchenProductionReturnDetail]
	SET
		[MasterId] = [KitchenProductionReturnDetails].[MasterId],
		[ProductId] = [KitchenProductionReturnDetails].[ProductId],
		[Quantity] = [KitchenProductionReturnDetails].[Quantity],
		[Rate] = [KitchenProductionReturnDetails].[Rate],
		[Total] = [KitchenProductionReturnDetails].[Total],
		[Remarks] = [KitchenProductionReturnDetails].[Remarks],
		[Status] = [KitchenProductionReturnDetails].[Status]
	FROM @KitchenProductionReturnDetails AS [KitchenProductionReturnDetails]
	WHERE [dbo].[KitchenProductionReturnDetail].[Id] = [KitchenProductionReturnDetails].[Id];

	INSERT INTO [dbo].[KitchenProductionReturnDetail]
	(
		[MasterId],
		[ProductId],
		[Quantity],
		[Rate],
		[Total],
		[Remarks],
		[Status]
	)
	SELECT
		[MasterId],
		[ProductId],
		[Quantity],
		[Rate],
		[Total],
		[Remarks],
		[Status]
	FROM @KitchenProductionReturnDetails
	WHERE [Id] = 0;
END;
CREATE PROCEDURE [dbo].[Insert_KitchenProductionDetail_List]
	@KitchenProductionDetails [dbo].[KitchenProductionDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[KitchenProductionDetail]
	SET
		[MasterId] = [KitchenProductionDetails].[MasterId],
		[ProductId] = [KitchenProductionDetails].[ProductId],
		[Quantity] = [KitchenProductionDetails].[Quantity],
		[Rate] = [KitchenProductionDetails].[Rate],
		[Total] = [KitchenProductionDetails].[Total],
		[Remarks] = [KitchenProductionDetails].[Remarks],
		[Status] = [KitchenProductionDetails].[Status]
	FROM @KitchenProductionDetails AS [KitchenProductionDetails]
	WHERE [dbo].[KitchenProductionDetail].[Id] = [KitchenProductionDetails].[Id];

	INSERT INTO [dbo].[KitchenProductionDetail]
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
	FROM @KitchenProductionDetails
	WHERE [Id] = 0;
END;
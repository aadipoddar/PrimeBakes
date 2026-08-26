CREATE PROCEDURE [dbo].[Insert_PurchaseOrderDetail_List]
	@PurchaseOrderDetails [dbo].[PurchaseOrderDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[PurchaseOrderDetail]
	SET
		[MasterId] = [PurchaseOrderDetails].[MasterId],
		[RawMaterialId] = [PurchaseOrderDetails].[RawMaterialId],
		[Quantity] = [PurchaseOrderDetails].[Quantity],
		[UnitOfMeasurement] = [PurchaseOrderDetails].[UnitOfMeasurement],
		[Remarks] = [PurchaseOrderDetails].[Remarks],
		[Status] = [PurchaseOrderDetails].[Status]
	FROM @PurchaseOrderDetails AS [PurchaseOrderDetails]
	WHERE [dbo].[PurchaseOrderDetail].[Id] = [PurchaseOrderDetails].[Id];

	INSERT INTO [dbo].[PurchaseOrderDetail]
	(
		[MasterId],
		[RawMaterialId],
		[Quantity],
		[UnitOfMeasurement],
		[Remarks],
		[Status]
	)
	SELECT
		[MasterId],
		[RawMaterialId],
		[Quantity],
		[UnitOfMeasurement],
		[Remarks],
		[Status]
	FROM @PurchaseOrderDetails
	WHERE [Id] = 0;
END;
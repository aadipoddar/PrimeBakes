CREATE PROCEDURE [dbo].[Insert_OrderDetail_List]
	@OrderDetails [dbo].[OrderDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[OrderDetail]
	SET
		[MasterId] = [OrderDetails].[MasterId],
		[ProductId] = [OrderDetails].[ProductId],
		[Quantity] = [OrderDetails].[Quantity],
		[Remarks] = [OrderDetails].[Remarks],
		[Status] = [OrderDetails].[Status]
	FROM @OrderDetails AS [OrderDetails]
	WHERE [dbo].[OrderDetail].[Id] = [OrderDetails].[Id];

	INSERT INTO [dbo].[OrderDetail]
	(
		[MasterId],
		[ProductId],
		[Quantity],
		[Remarks],
		[Status]
	)
	SELECT
		[MasterId],
		[ProductId],
		[Quantity],
		[Remarks],
		[Status]
	FROM @OrderDetails
	WHERE [Id] = 0;
END;
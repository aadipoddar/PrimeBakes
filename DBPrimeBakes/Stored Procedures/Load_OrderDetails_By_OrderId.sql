CREATE PROCEDURE [dbo].[Load_OrderDetails_By_OrderId]
	@OrderId INT
AS
BEGIN

	SELECT
		[OrderId],
		[ItemId],
		[ItemName],
		[ItemCode],
		[CategoryId],
		[CategoryName],
		[CategoryCode],
		[Quantity]
	FROM View_OrderDetails
	WHERE OrderId = @OrderId

END

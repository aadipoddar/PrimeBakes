CREATE PROCEDURE Delete_OrderDetails
	@OrderId int
AS
BEGIN
	DELETE FROM OrderDetail
	WHERE OrderId = @OrderId
END
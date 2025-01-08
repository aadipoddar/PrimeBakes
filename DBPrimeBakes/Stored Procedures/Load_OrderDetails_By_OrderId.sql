CREATE PROCEDURE [Load_OrderDetails_By_OrderId]
	@OrderId INT
AS
BEGIN

	SELECT
		it.Id ItemId,
		it.Name ItemName,
		it.Code ItemCode,
		ct.Id CategoryId,
		ct.Code CategoryCode,
		ct.Name CategoryName,
		odt.Quantity
	FROM OrderDetail odt
	JOIN Item it ON odt.ItemId = it.Id
	JOIN Category ct ON it.CategoryId = ct.Id
	WHERE odt.OrderId = @OrderId

END;
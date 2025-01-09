CREATE VIEW [dbo].[View_OrderDetails]
	AS
	SELECT
		odt.OrderId Id,
		it.Id ItemId,
		it.Name ItemName,
		it.Code ItemCode,
		ct.Id CategoryId,
		ct.Name CategoryName,
		ct.Code CategoryCode,
		odt.Quantity
	FROM OrderDetail odt
	JOIN Item it ON odt.ItemId = it.Id
	JOIN Category ct ON it.CategoryId = ct.Id
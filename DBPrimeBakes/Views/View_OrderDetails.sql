CREATE VIEW [dbo].[View_OrderDetails]
	AS
	SELECT
		odt.OrderId,
		it.Id ItemId,
		it.Name ItemName,
		it.Code ItemCode,
		ct.Id CategoryId,
		ct.Name CategoryName,
		ct.Code CategoryCode,
		odt.Quantity
	FROM OrderDetail odt
	JOIN Item it ON odt.ItemId = it.Id
	JOIN [ItemCategory] ct ON it.[ItemCategoryId] = ct.Id
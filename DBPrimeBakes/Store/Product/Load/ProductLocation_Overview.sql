CREATE VIEW [dbo].[ProductLocation_Overview]
	AS
SELECT
	pl.Id,
	pl.ProductId,
	p.Code,
	p.Name,
	p.ProductCategoryId,
	pl.Rate,
	p.TaxId,
	p.FoodType,
	p.ShowInMenu,
	p.Remarks,
	pl.LocationId

FROM ProductLocation pl

INNER JOIN Product p ON pl.ProductId = p.Id

WHERE p.[Status] = 1;
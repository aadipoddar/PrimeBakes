CREATE VIEW [dbo].[Recipe_Item_Overview]
AS
SELECT
	[rd].[Id],
	[rd].[RawMaterialId] AS ItemId,
	[rm].[Name] AS ItemName,
	[rm].[Code] AS ItemCode,
	[rm].[RawMaterialCategoryId] AS ItemCategoryId,
	[rc].[Name] AS ItemCategoryName,
	[rm].[UnitOfMeasurement],

	[rd].[Quantity],
	[rm].[Rate],
	[rd].[Quantity] * [rm].[Rate] AS Amount,
	CASE
		WHEN [r].[Quantity] > 0 THEN ([rd].[Quantity] * [rm].[Rate]) / [r].[Quantity]
		ELSE 0
	END AS PerUnit,

	[rd].[MasterId],
	[r].[ProductId],
	[p].[Name] AS ProductName,
	[r].[Quantity] AS RecipeQuantity,
	[r].[Deduct],

	[r].[Status] AS MasterStatus

FROM
	[dbo].[RecipeDetail] rd
INNER JOIN
	[dbo].[Recipe] r ON rd.MasterId = r.Id
INNER JOIN
	[dbo].[Product] p ON r.ProductId = p.Id
INNER JOIN
	[dbo].[RawMaterial] rm ON rd.RawMaterialId = rm.Id
INNER JOIN
	[dbo].[RawMaterialCategory] rc ON rm.RawMaterialCategoryId = rc.Id

WHERE
	[rd].[Status] = 1;

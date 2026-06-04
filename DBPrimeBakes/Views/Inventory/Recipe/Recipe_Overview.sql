CREATE VIEW [dbo].[Recipe_Overview]
	AS
	SELECT
		[r].[Id],
		[r].[ProductId],
		[p].[Name] AS ProductName,
		[r].[Quantity],
		[r].[Deduct],
		COUNT([rd].[Id]) AS ItemCount,
		ISNULL(SUM([rd].[Quantity] * [rm].[Rate]), 0) AS TotalCost,
		CASE
			WHEN [r].[Quantity] > 0 THEN ISNULL(SUM([rd].[Quantity] * [rm].[Rate]), 0) / [r].[Quantity]
			ELSE 0
		END AS PerUnitCost,
		[r].[Status]

	FROM
		[dbo].[Recipe] r
	INNER JOIN
		[dbo].[Product] p ON r.ProductId = p.Id
	LEFT JOIN
		[dbo].[RecipeDetail] rd ON rd.MasterId = r.Id AND rd.Status = 1
	LEFT JOIN
		[dbo].[RawMaterial] rm ON rm.Id = rd.RawMaterialId AND rm.Status = 1

	GROUP BY
		[r].[Id],
		[r].[ProductId],
		[p].[Name],
		[r].[Quantity],
		[r].[Deduct],
		[r].[Status]

CREATE VIEW [dbo].[Bill_Item_Overview]
	AS
SELECT
	[pr].[Id],
	[pr].[Name] AS ItemName,
	[pr].[Code] AS ItemCode,
	[pc].[Id] AS ItemCategoryId,
	[pc].[Name] AS ItemCategoryName,

	[b].[Id] AS MasterId,
	[b].[TransactionNo],
	[b].[TransactionDateTime],
	[c].[Id] AS CompanyId,
	[c].[Name] AS CompanyName,
	[l].[Id] AS LocationId,
	[l].[Name] AS LocationName,

	[dt].[Id] AS DiningTableId,
	[dt].[Name] AS DiningTableName,
	[da].[Name] AS DiningAreaName,

	[cust].[Id] AS CustomerId,
	[cust].[Name] AS CustomerName,

	[b].[Remarks] AS BillRemarks,

	[bd].[Quantity],
	[bd].[Rate],
	[bd].[BaseTotal],

	[bd].[DiscountPercent],
	[bd].[DiscountAmount],
	[bd].[AfterDiscount],

	[bd].[CGSTPercent],
	[bd].[CGSTAmount],
	[bd].[SGSTPercent],
	[bd].[SGSTAmount],
	[bd].[IGSTPercent],
	[bd].[IGSTAmount],
	[bd].[TotalTaxAmount],
	[bd].[InclusiveTax],

	[bd].[Total],
	[bd].[NetRate],
	[bd].[NetRate] * [bd].[Quantity] AS NetTotal,

	[bd].[Remarks]

FROM
	[dbo].[BillDetail] bd

INNER JOIN
	[dbo].[Bill] b ON bd.[MasterId] = b.Id
INNER JOIN
	[dbo].[Product] pr ON bd.ProductId = pr.Id
INNER JOIN
	[dbo].[ProductCategory] pc ON pr.ProductCategoryId = pc.Id
INNER JOIN
	[dbo].[Company] c ON b.CompanyId = c.Id
INNER JOIN
	[dbo].[Location] l ON b.LocationId = l.Id
INNER JOIN
	[dbo].[DiningTable] dt ON b.DiningTableId = dt.Id
INNER JOIN
	[dbo].[DiningArea] da ON dt.DiningAreaId = da.Id
LEFT JOIN
	[dbo].[Customer] cust ON b.CustomerId = cust.Id

WHERE
	[b].[Status] = 1 AND
	[bd].[Status] = 1;

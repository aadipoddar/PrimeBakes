CREATE VIEW [dbo].[Order_Item_Overview]
	AS
SELECT
	[od].[Id],
	[od].[ProductId] AS ItemId,
	[pr].[Name] AS ItemName,
	[pr].[Code] AS ItemCode,
	[pc].[Id] AS ItemCategoryId,
	[pc].[Name] AS ItemCategoryName,

	[od].[Quantity],
	[od].[Remarks] AS ItemRemarks,

	[od].[MasterId],
	[o].[TransactionNo],
	[o].[CompanyId],
	[c].[Name] AS CompanyName,
	[o].[LocationId],
	[l].[Name] AS LocationName,

	[o].[SaleId],
	[s].[TransactionNo] AS SaleTransactionNo,
	[s].[TransactionDateTime] AS SaleDateTime,

	[o].[TransactionDateTime],
	[o].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

	[o].[TotalItems],
	[o].[TotalQuantity],

	[o].[Remarks],
	[o].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[o].[CreatedAt],
	[o].[CreatedFromPlatform],
	[o].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[o].[LastModifiedAt],
	[o].[LastModifiedFromPlatform],

	[o].[Status] AS MasterStatus

FROM
	[dbo].[OrderDetail] od

INNER JOIN
	[dbo].[Order] o ON od.[MasterId] = o.Id
INNER JOIN
	[dbo].[Product] pr ON od.ProductId = pr.Id
INNER JOIN
	[dbo].[ProductCategory] pc ON pr.ProductCategoryId = pc.Id
INNER JOIN
	[dbo].[Company] c ON o.CompanyId = c.Id
INNER JOIN
	[dbo].[Location] l ON o.LocationId = l.Id
LEFT JOIN
	[dbo].[Sale] s ON o.SaleId = s.Id
INNER JOIN
	[dbo].[FinancialYear] AS fy ON o.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[User] AS u ON o.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON o.LastModifiedBy = lm.Id

WHERE
	[od].[Status] = 1;

CREATE VIEW [dbo].[Bill_Item_Overview]
	AS
SELECT
	[bd].[Id],
	[bd].[ProductId] AS ItemId,
	[pr].[Name] AS ItemName,
	[pr].[Code] AS ItemCode,
	[pc].[Id] AS ItemCategoryId,
	[pc].[Name] AS ItemCategoryName,

	[bd].[Quantity],
	[bd].[Rate],
	[bd].[BaseTotal] AS ItemBaseTotal,

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
	[bd].[Remarks] AS ItemRemarks,
	[bd].[KOTPrint],

	[b].[Id] AS MasterId,
	[b].[TransactionNo],
	[b].[CompanyId],
	[c].[Name] AS CompanyName,
	[b].[LocationId],
	[l].[Name] AS LocationName,

	[b].[DiningTableId],
	[dt].[Name] AS DiningTableName,
	[da].[Name] AS DiningAreaName,

	[b].[CustomerId],
	[cust].[Name] AS CustomerName,

	[b].[TransactionDateTime],
	[b].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

	[b].[TotalPeople],
	[b].[TotalItems],
	[b].[TotalQuantity],
	[b].[BaseTotal],
	[b].[ItemDiscountAmount],
	[b].[TotalAfterItemDiscount],
	[b].[TotalInclusiveTaxAmount],
	[b].[TotalExtraTaxAmount],
	[b].[TotalAfterTax],

	[b].[DiscountPercent] AS BillDiscountPercent,
	[b].[DiscountAmount] AS BillDiscountAmount,
	[b].[ServiceChargePercent],
	[b].[ServiceChargeAmount],

	[b].[RoundOffAmount],
	[b].[TotalAmount],

	[b].[Cash],
	[b].[Card],
	[b].[UPI],
	[b].[Credit],

	STUFF(
		CONCAT(
			CASE WHEN [b].[Cash] > 0 THEN ',Cash' ELSE '' END,
			CASE WHEN [b].[Card] > 0 THEN ',Card' ELSE '' END,
			CASE WHEN [b].[UPI] > 0 THEN ',UPI' ELSE '' END,
			CASE WHEN [b].[Credit] > 0 THEN ',Credit' ELSE '' END
		), 1, 1, ''
	) AS PaymentModes,

	[b].[Remarks],
	[b].[Running],
	[b].[FinancialAccountingId],
	[fa].[TransactionNo] AS FinancialAccountingTransactionNo,
	[b].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[b].[CreatedAt],
	[b].[CreatedFromPlatform],
	[b].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[b].[LastModifiedAt],
	[b].[LastModifiedFromPlatform],

	[b].[Status] AS MasterStatus

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
INNER JOIN
	[dbo].[FinancialYear] AS fy ON b.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[User] AS u ON b.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON b.LastModifiedBy = lm.Id
LEFT JOIN
	[dbo].[FinancialAccounting] AS fa ON b.FinancialAccountingId = fa.Id

WHERE
	[bd].[Status] = 1;

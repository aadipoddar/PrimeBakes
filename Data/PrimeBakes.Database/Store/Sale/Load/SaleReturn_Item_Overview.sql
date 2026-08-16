CREATE VIEW [dbo].[SaleReturn_Item_Overview]
	AS
SELECT
	[sd].[Id],
	[sd].[ProductId] AS ItemId,
	[pr].[Name] AS ItemName,
	[pr].[Code] AS ItemCode,
	[pc].[Id] AS ItemCategoryId,
	[pc].[Name] AS ItemCategoryName,

	[sd].[Quantity],
	[sd].[Rate],
	[sd].[BaseTotal] AS ItemBaseTotal,

	[sd].[DiscountPercent],
	[sd].[DiscountAmount],
	[sd].[AfterDiscount],

	[sd].[CGSTPercent],
	[sd].[CGSTAmount],
	[sd].[SGSTPercent],
	[sd].[SGSTAmount],
	[sd].[IGSTPercent],
	[sd].[IGSTAmount],
	[sd].[TotalTaxAmount],
	[sd].[InclusiveTax],

	[sd].[Total],
	[sd].[NetRate],
	[sd].[NetRate] * [sd].[Quantity] AS NetTotal,
	[sd].[Remarks] AS ItemRemarks,

	[sr].[Id] AS MasterId,
	[sr].[TransactionNo],
	[sr].[CompanyId],
	[c].[Name] AS CompanyName,
	[sr].[LocationId],
	[l].[Name] AS LocationName,

	[sr].[PartyId],
	[p].[Name] AS PartyName,
	[sr].[CustomerId],
	[cust].[Name] AS CustomerName,

	[sr].[TransactionDateTime],
	[sr].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

	[sr].[TotalItems],
	[sr].[TotalQuantity],
	[sr].[BaseTotal],
	[sr].[ItemDiscountAmount],
	[sr].[TotalAfterItemDiscount],
	[sr].[TotalInclusiveTaxAmount],
	[sr].[TotalExtraTaxAmount],
	[sr].[TotalAfterTax],

	[sr].[OtherChargesPercent],
	[sr].[OtherChargesAmount],
	[sr].[DiscountPercent] AS SaleReturnDiscountPercent,
	[sr].[DiscountAmount] AS SaleReturnDiscountAmount,

	[sr].[RoundOffAmount],
	[sr].[TotalAmount],

	[sr].[Cash],
	[sr].[Card],
	[sr].[UPI],
	[sr].[Credit],

	STUFF(
		CONCAT(
			CASE WHEN [sr].[Cash] > 0 THEN ',Cash' ELSE '' END,
			CASE WHEN [sr].[Card] > 0 THEN ',Card' ELSE '' END,
			CASE WHEN [sr].[UPI] > 0 THEN ',UPI' ELSE '' END,
			CASE WHEN [sr].[Credit] > 0 THEN ',Credit' ELSE '' END
		), 1, 1, ''
	) AS PaymentModes,

	[sr].[Remarks],
	[sr].[FinancialAccountingId],
	[fa].[TransactionNo] AS FinancialAccountingTransactionNo,
	[sr].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[sr].[CreatedAt],
	[sr].[CreatedFromPlatform],
	[sr].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[sr].[LastModifiedAt],
	[sr].[LastModifiedFromPlatform],

	[sr].[Status] AS MasterStatus

FROM
	[dbo].[SaleReturnDetail] sd

INNER JOIN
	[dbo].[SaleReturn] sr ON sd.[MasterId] = sr.Id
INNER JOIN
	[dbo].[Product] pr ON sd.ProductId = pr.Id
INNER JOIN
	[dbo].[ProductCategory] pc ON pr.ProductCategoryId = pc.Id
INNER JOIN
	[dbo].[Company] c ON sr.CompanyId = c.Id
INNER JOIN
	[dbo].[Location] l ON sr.LocationId = l.Id
LEFT JOIN
	[dbo].[Ledger] p ON sr.PartyId = p.Id
LEFT JOIN
	[dbo].[Customer] cust ON sr.CustomerId = cust.Id
INNER JOIN
	[dbo].[FinancialYear] AS fy ON sr.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[User] AS u ON sr.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON sr.LastModifiedBy = lm.Id
LEFT JOIN
	[dbo].[FinancialAccounting] AS fa ON sr.FinancialAccountingId = fa.Id

WHERE
	[sd].[Status] = 1;

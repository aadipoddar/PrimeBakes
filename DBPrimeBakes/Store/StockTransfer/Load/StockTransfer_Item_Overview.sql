CREATE VIEW [dbo].[StockTransfer_Item_Overview]
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

	[s].[Id] AS MasterId,
	[s].[TransactionNo],
	[s].[CompanyId],
	[c].[Name] AS CompanyName,

	[s].[LocationId],
	[fl].[Name] AS LocationName,

	[s].[ToLocationId],
	[tl].[Name] AS ToLocationName,

	[s].[TransactionDateTime],
	[s].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

	[s].[TotalItems],
	[s].[TotalQuantity],
	[s].[BaseTotal],
	[s].[ItemDiscountAmount],
	[s].[TotalAfterItemDiscount],
	[s].[TotalInclusiveTaxAmount],
	[s].[TotalExtraTaxAmount],
	[s].[TotalAfterTax],

	[s].[OtherChargesPercent],
	[s].[OtherChargesAmount],
	[s].[DiscountPercent] AS StockTransferDiscountPercent,
	[s].[DiscountAmount] AS StockTransferDiscountAmount,

	[s].[RoundOffAmount],
	[s].[TotalAmount],

	[s].[Cash],
	[s].[Card],
	[s].[UPI],
	[s].[Credit],

	STUFF(
		CONCAT(
			CASE WHEN [s].[Cash] > 0 THEN ',Cash' ELSE '' END,
			CASE WHEN [s].[Card] > 0 THEN ',Card' ELSE '' END,
			CASE WHEN [s].[UPI] > 0 THEN ',UPI' ELSE '' END,
			CASE WHEN [s].[Credit] > 0 THEN ',Credit' ELSE '' END
		), 1, 1, ''
	) AS PaymentModes,

	[s].[Remarks],
	[s].[FinancialAccountingId],
	[fa].[TransactionNo] AS FinancialAccountingTransactionNo,
	[s].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[s].[CreatedAt],
	[s].[CreatedFromPlatform],
	[s].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[s].[LastModifiedAt],
	[s].[LastModifiedFromPlatform],

	[s].[Status] AS MasterStatus

FROM
	[dbo].[StockTransferDetail] sd

INNER JOIN
	[dbo].[StockTransfer] s ON sd.[MasterId] = s.Id
INNER JOIN
	[dbo].[Product] pr ON sd.ProductId = pr.Id
INNER JOIN
	[dbo].[ProductCategory] pc ON pr.ProductCategoryId = pc.Id
INNER JOIN
	[dbo].[Company] c ON s.CompanyId = c.Id
INNER JOIN
	[dbo].[Location] fl ON s.[LocationId] = fl.Id
INNER JOIN
	[dbo].[Location] tl ON s.ToLocationId = tl.Id
INNER JOIN
	[dbo].[FinancialYear] AS fy ON s.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[User] AS u ON s.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON s.LastModifiedBy = lm.Id
LEFT JOIN
	[dbo].[FinancialAccounting] AS fa ON s.FinancialAccountingId = fa.Id

WHERE
	[sd].[Status] = 1;

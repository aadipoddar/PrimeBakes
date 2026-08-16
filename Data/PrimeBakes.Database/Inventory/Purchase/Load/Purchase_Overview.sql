CREATE VIEW [dbo].[Purchase_Overview]
AS
SELECT
    [p].[Id],
    [p].[TransactionNo],
    [p].[CompanyId],
    [c].[Name] AS CompanyName,

    [p].[TransactionDateTime],
    [p].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

	[p].[ChallanNo],
	[p].[PartyId],
	[l].[Name] AS PartyName,

	[p].[TotalItems],
	[p].[TotalQuantity],
	[p].[BaseTotal],
	[p].[ItemDiscountAmount],
	[p].[TotalAfterItemDiscount],
	[p].[TotalInclusiveTaxAmount],
	[p].[TotalExtraTaxAmount],
	[p].[TotalAfterTax],

	[p].[OtherChargesPercent],
	[p].[OtherChargesAmount],
	[p].[CashDiscountPercent],
	[p].[CashDiscountAmount],

	[p].[RoundOffAmount],
	[p].[TotalAmount],

    [p].[Remarks],
	[p].[DocumentUrl],
	[p].[FinancialAccountingId],
	[fa].[TransactionNo] AS FinancialAccountingTransactionNo,
	[p].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[p].[CreatedAt],
	[p].[CreatedFromPlatform],
	[p].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[p].[LastModifiedAt],
	[p].[LastModifiedFromPlatform],

	[p].[Status]

FROM
    [dbo].[Purchase] p
INNER JOIN
    [dbo].[Company] c ON p.CompanyId = c.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON p.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[Ledger] l ON p.PartyId = l.Id
LEFT JOIN
	[dbo].[FinancialAccounting] fa ON p.FinancialAccountingId = fa.Id
INNER JOIN
	[dbo].[User] AS u ON p.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON p.LastModifiedBy = lm.Id

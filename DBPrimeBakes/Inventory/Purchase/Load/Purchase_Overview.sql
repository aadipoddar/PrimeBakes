CREATE VIEW [dbo].[Purchase_Overview]
AS
SELECT
    [t].[Id],
    [t].[TransactionNo],
    [t].[CompanyId],
    [c].[Name] AS CompanyName,

    [t].[TransactionDateTime],
    [t].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

	[t].[ChallanNo],
	[t].[PartyId],
	[l].[Name] AS PartyName,

	[t].[TotalItems],
	[t].[TotalQuantity],
	[t].[BaseTotal],
	[t].[ItemDiscountAmount],
	[t].[TotalAfterItemDiscount],
	[t].[TotalInclusiveTaxAmount],
	[t].[TotalExtraTaxAmount],
	[t].[TotalAfterTax],

	[t].[OtherChargesPercent],
	[t].[OtherChargesAmount],
	[t].[CashDiscountPercent],
	[t].[CashDiscountAmount],

	[t].[RoundOffAmount],
	[t].[TotalAmount],

    [t].[Remarks],
	[t].[DocumentUrl],
	[t].[FinancialAccountingId],
	[fa].[TransactionNo] AS FinancialAccountingTransactionNo,
	[t].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[t].[CreatedAt],
	[t].[CreatedFromPlatform],
	[t].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[t].[LastModifiedAt],
	[t].[LastModifiedFromPlatform],

	[t].[Status]

FROM
    [dbo].[Purchase] t
INNER JOIN
    [dbo].[Company] c ON t.CompanyId = c.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON t.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[Ledger] l ON t.PartyId = l.Id
LEFT JOIN
	[dbo].[FinancialAccounting] fa ON t.FinancialAccountingId = fa.Id
INNER JOIN
	[dbo].[User] AS u ON t.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON t.LastModifiedBy = lm.Id

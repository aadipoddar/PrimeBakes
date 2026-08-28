CREATE VIEW [dbo].[PurchaseReturn_Overview]
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
	[p].[CreatedFormFactor],
	[p].[CreatedPlatform],
	[p].[CreatedLatitude],
	[p].[CreatedLongitude],
	[p].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[p].[LastModifiedAt],
	[p].[LastModifiedFormFactor],
	[p].[LastModifiedPlatform],
	[p].[LastModifiedLatitude],
	[p].[LastModifiedLongitude],

	CASE WHEN [p].[CreatedLatitude] IS NOT NULL AND [ul].[Latitude] IS NOT NULL THEN geography::Point([p].[CreatedLatitude], [p].[CreatedLongitude], 4326).STDistance(geography::Point([ul].[Latitude], [ul].[Longitude], 4326)) END AS CreatedUserOffset,
	CASE WHEN [p].[LastModifiedLatitude] IS NOT NULL AND [lml].[Latitude] IS NOT NULL THEN geography::Point([p].[LastModifiedLatitude], [p].[LastModifiedLongitude], 4326).STDistance(geography::Point([lml].[Latitude], [lml].[Longitude], 4326)) END AS LastModifiedUserOffset,

	[p].[Status]

FROM
    [dbo].[PurchaseReturn] p
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
LEFT JOIN
	[dbo].[Location] AS ul ON u.LocationId = ul.Id
LEFT JOIN
	[dbo].[Location] AS lml ON lm.LocationId = lml.Id
CREATE VIEW [dbo].[Bill_Overview]
	AS
SELECT
	[b].[Id],
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

	[b].[DiscountPercent],
	[b].[DiscountAmount],
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
	[b].[FinancialAccountingId],
	[fa].[TransactionNo] AS FinancialAccountingTransactionNo,
	[b].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[b].[CreatedAt],
	[b].[CreatedFormFactor],
	[b].[CreatedPlatform],
	[b].[CreatedLatitude],
	[b].[CreatedLongitude],
	[b].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[b].[LastModifiedAt],
	[b].[LastModifiedFormFactor],
	[b].[LastModifiedPlatform],
	[b].[LastModifiedLatitude],
	[b].[LastModifiedLongitude],

	CASE WHEN [b].[CreatedLatitude] IS NOT NULL AND [l].[Latitude] IS NOT NULL THEN geography::Point([b].[CreatedLatitude], [b].[CreatedLongitude], 4326).STDistance(geography::Point([l].[Latitude], [l].[Longitude], 4326)) END AS CreatedLocationOffset,
	CASE WHEN [b].[CreatedLatitude] IS NOT NULL AND [ul].[Latitude] IS NOT NULL THEN geography::Point([b].[CreatedLatitude], [b].[CreatedLongitude], 4326).STDistance(geography::Point([ul].[Latitude], [ul].[Longitude], 4326)) END AS CreatedUserOffset,
	CASE WHEN [b].[LastModifiedLatitude] IS NOT NULL AND [l].[Latitude] IS NOT NULL THEN geography::Point([b].[LastModifiedLatitude], [b].[LastModifiedLongitude], 4326).STDistance(geography::Point([l].[Latitude], [l].[Longitude], 4326)) END AS LastModifiedLocationOffset,
	CASE WHEN [b].[LastModifiedLatitude] IS NOT NULL AND [lml].[Latitude] IS NOT NULL THEN geography::Point([b].[LastModifiedLatitude], [b].[LastModifiedLongitude], 4326).STDistance(geography::Point([lml].[Latitude], [lml].[Longitude], 4326)) END AS LastModifiedUserOffset,

	[b].[Running],
	[b].[Status]

FROM
	[dbo].[Bill] AS b
INNER JOIN
	[dbo].[Company] AS c ON b.CompanyId = c.Id
INNER JOIN
	[dbo].[Location] AS l ON b.LocationId = l.Id
INNER JOIN
	[dbo].[DiningTable] AS dt ON b.DiningTableId = dt.Id
INNER JOIN
	[dbo].[DiningArea] AS da ON dt.DiningAreaId = da.Id
LEFT JOIN
	[dbo].[Customer] AS cust ON b.CustomerId = cust.Id
INNER JOIN
	[dbo].[FinancialYear] AS fy ON b.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[User] AS u ON b.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON b.LastModifiedBy = lm.Id
LEFT JOIN
	[dbo].[Location] AS ul ON u.LocationId = ul.Id
LEFT JOIN
	[dbo].[Location] AS lml ON lm.LocationId = lml.Id
LEFT JOIN
	[dbo].[FinancialAccounting] AS fa ON b.FinancialAccountingId = fa.Id

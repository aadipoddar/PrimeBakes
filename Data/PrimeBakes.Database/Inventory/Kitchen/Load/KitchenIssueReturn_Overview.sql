CREATE VIEW [dbo].[KitchenIssueReturn_Overview]
AS
SELECT
    [kir].[Id],
    [kir].[TransactionNo],
    [kir].[CompanyId],
    [c].[Name] AS CompanyName,

    [kir].[TransactionDateTime],
    [kir].[FinancialYearId],
    CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

    [kir].[KitchenId],
    [k].[Name] AS KitchenName,

    [kir].[TotalItems],
    [kir].[TotalQuantity],
    [kir].[TotalAmount],

    [kir].[Remarks],
    [kir].[CreatedBy],
    [u].[Name] AS CreatedByName,
    [kir].[CreatedAt],
    [kir].[CreatedFormFactor],
	[kir].[CreatedPlatform],
	[kir].[CreatedLatitude],
	[kir].[CreatedLongitude],
    [kir].[LastModifiedBy],
    [lm].[Name] AS LastModifiedByUserName,
    [kir].[LastModifiedAt],
    [kir].[LastModifiedFormFactor],
	[kir].[LastModifiedPlatform],
	[kir].[LastModifiedLatitude],
	[kir].[LastModifiedLongitude],

	CASE WHEN [kir].[CreatedLatitude] IS NOT NULL AND [ul].[Latitude] IS NOT NULL THEN geography::Point([kir].[CreatedLatitude], [kir].[CreatedLongitude], 4326).STDistance(geography::Point([ul].[Latitude], [ul].[Longitude], 4326)) END AS CreatedUserOffset,
	CASE WHEN [kir].[LastModifiedLatitude] IS NOT NULL AND [lml].[Latitude] IS NOT NULL THEN geography::Point([kir].[LastModifiedLatitude], [kir].[LastModifiedLongitude], 4326).STDistance(geography::Point([lml].[Latitude], [lml].[Longitude], 4326)) END AS LastModifiedUserOffset,

    [kir].[Status]

FROM
    [dbo].[KitchenIssueReturn] kir
INNER JOIN
    [dbo].[Company] c ON kir.CompanyId = c.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON kir.FinancialYearId = fy.Id
INNER JOIN
    [dbo].[Kitchen] k ON kir.KitchenId = k.Id
INNER JOIN
    [dbo].[User] u ON kir.CreatedBy = u.Id
LEFT JOIN
    [dbo].[User] lm ON kir.LastModifiedBy = lm.Id
LEFT JOIN
    [dbo].[Location] ul ON u.LocationId = ul.Id
LEFT JOIN
    [dbo].[Location] lml ON lm.LocationId = lml.Id

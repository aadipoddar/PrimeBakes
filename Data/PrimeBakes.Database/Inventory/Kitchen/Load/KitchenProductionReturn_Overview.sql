CREATE VIEW [dbo].[KitchenProductionReturn_Overview]
AS
SELECT
    [kpr].[Id],
    [kpr].[TransactionNo],
    [kpr].[CompanyId],
    [c].[Name] AS CompanyName,

    [kpr].[TransactionDateTime],
    [kpr].[FinancialYearId],
    CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

    [kpr].[KitchenId],
    [k].[Name] AS KitchenName,

    [kpr].[TotalItems],
    [kpr].[TotalQuantity],
    [kpr].[TotalAmount],

    [kpr].[Remarks],
    [kpr].[CreatedBy],
    [u].[Name] AS CreatedByName,
    [kpr].[CreatedAt],
    [kpr].[CreatedFormFactor],
	[kpr].[CreatedPlatform],
	[kpr].[CreatedLatitude],
	[kpr].[CreatedLongitude],
    [kpr].[LastModifiedBy],
    [lm].[Name] AS LastModifiedByUserName,
    [kpr].[LastModifiedAt],
    [kpr].[LastModifiedFormFactor],
	[kpr].[LastModifiedPlatform],
	[kpr].[LastModifiedLatitude],
	[kpr].[LastModifiedLongitude],

	CASE WHEN [kpr].[CreatedLatitude] IS NOT NULL AND [ul].[Latitude] IS NOT NULL THEN geography::Point([kpr].[CreatedLatitude], [kpr].[CreatedLongitude], 4326).STDistance(geography::Point([ul].[Latitude], [ul].[Longitude], 4326)) END AS CreatedUserOffset,
	CASE WHEN [kpr].[LastModifiedLatitude] IS NOT NULL AND [lml].[Latitude] IS NOT NULL THEN geography::Point([kpr].[LastModifiedLatitude], [kpr].[LastModifiedLongitude], 4326).STDistance(geography::Point([lml].[Latitude], [lml].[Longitude], 4326)) END AS LastModifiedUserOffset,

    [kpr].[Status]

FROM
    [dbo].[KitchenProductionReturn] kpr
INNER JOIN
    [dbo].[Company] c ON kpr.CompanyId = c.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON kpr.FinancialYearId = fy.Id
INNER JOIN
    [dbo].[Kitchen] k ON kpr.KitchenId = k.Id
INNER JOIN
    [dbo].[User] u ON kpr.CreatedBy = u.Id
LEFT JOIN
    [dbo].[User] lm ON kpr.LastModifiedBy = lm.Id
LEFT JOIN
    [dbo].[Location] ul ON u.LocationId = ul.Id
LEFT JOIN
    [dbo].[Location] lml ON lm.LocationId = lml.Id

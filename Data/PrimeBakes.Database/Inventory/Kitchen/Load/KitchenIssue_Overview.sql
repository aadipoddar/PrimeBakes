CREATE VIEW [dbo].[KitchenIssue_Overview]
AS
SELECT
    [ki].[Id],
    [ki].[TransactionNo],
    [ki].[CompanyId],
    [c].[Name] AS CompanyName,

    [ki].[TransactionDateTime],
    [ki].[FinancialYearId],
    CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

    [ki].[KitchenId],
    [k].[Name] AS KitchenName,

    [ki].[TotalItems],
    [ki].[TotalQuantity],
    [ki].[TotalAmount],

    [ki].[Remarks],
    [ki].[CreatedBy],
    [u].[Name] AS CreatedByName,
    [ki].[CreatedAt],
    [ki].[CreatedFormFactor],
	[ki].[CreatedPlatform],
	[ki].[CreatedLatitude],
	[ki].[CreatedLongitude],
    [ki].[LastModifiedBy],
    [lm].[Name] AS LastModifiedByUserName,
    [ki].[LastModifiedAt],
    [ki].[LastModifiedFormFactor],
	[ki].[LastModifiedPlatform],
	[ki].[LastModifiedLatitude],
	[ki].[LastModifiedLongitude],

	CASE WHEN [ki].[CreatedLatitude] IS NOT NULL AND [ul].[Latitude] IS NOT NULL THEN geography::Point([ki].[CreatedLatitude], [ki].[CreatedLongitude], 4326).STDistance(geography::Point([ul].[Latitude], [ul].[Longitude], 4326)) END AS CreatedUserOffset,
	CASE WHEN [ki].[LastModifiedLatitude] IS NOT NULL AND [lml].[Latitude] IS NOT NULL THEN geography::Point([ki].[LastModifiedLatitude], [ki].[LastModifiedLongitude], 4326).STDistance(geography::Point([lml].[Latitude], [lml].[Longitude], 4326)) END AS LastModifiedUserOffset,

    [ki].[Status]

FROM
    [dbo].[KitchenIssue] ki
INNER JOIN
    [dbo].[Company] c ON ki.CompanyId = c.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON ki.FinancialYearId = fy.Id
INNER JOIN
    [dbo].[Kitchen] k ON ki.KitchenId = k.Id
INNER JOIN
    [dbo].[User] u ON ki.CreatedBy = u.Id
LEFT JOIN
    [dbo].[User] lm ON ki.LastModifiedBy = lm.Id
LEFT JOIN
    [dbo].[Location] ul ON u.LocationId = ul.Id
LEFT JOIN
    [dbo].[Location] lml ON lm.LocationId = lml.Id
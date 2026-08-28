CREATE VIEW [dbo].[KitchenProductionReturn_Item_Overview]
AS
SELECT
    [kprd].[Id],
    [kprd].[ProductId] AS ItemId,
    [p].[Name] AS ItemName,
    [p].[Code] AS ItemCode,
    [p].[ProductCategoryId] AS ItemCategoryId,
    [pc].[Name] AS ItemCategoryName,

    [kprd].[Quantity],
    [kprd].[Rate],
    [kprd].[Total],

    [kprd].[Remarks] AS ItemRemarks,

    [kprd].[MasterId],
    [kpr].[TransactionNo],
    [kpr].[CompanyId],
    [c].[Name] AS CompanyName,

    [kpr].[TransactionDateTime],
    [kpr].[FinancialYearId],
    CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

    [kpr].[KitchenId],
    [k].[Name] AS KitchenName,
    [kpr].[Remarks] AS KitchenProductionReturnRemarks,

    [kpr].[TotalItems],
    [kpr].[TotalQuantity],
    [kpr].[TotalAmount],

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

    [kpr].[Status] AS MasterStatus

FROM
    [dbo].[KitchenProductionReturnDetail] kprd
INNER JOIN
    [dbo].[KitchenProductionReturn] kpr ON kprd.MasterId = kpr.Id
INNER JOIN
    [dbo].[Product] p ON kprd.ProductId = p.Id
INNER JOIN
    [dbo].[ProductCategory] pc ON p.ProductCategoryId = pc.Id
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

WHERE
    [kprd].[Status] = 1;

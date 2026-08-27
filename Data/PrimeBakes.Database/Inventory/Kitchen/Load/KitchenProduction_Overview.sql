CREATE VIEW [dbo].[KitchenProduction_Overview]
AS
SELECT
    [kp].[Id],
    [kp].[TransactionNo],
    [kp].[CompanyId],
    [c].[Name] AS CompanyName,

    [kp].[TransactionDateTime],
    [kp].[FinancialYearId],
    CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

    [kp].[KitchenId],
    [k].[Name] AS KitchenName,

    [kp].[TotalItems],
    [kp].[TotalQuantity],
    [kp].[TotalAmount],

    [kp].[Remarks],
    [kp].[CreatedBy],
    [u].[Name] AS CreatedByName,
    [kp].[CreatedAt],
    [kp].[CreatedFormFactor],
	[kp].[CreatedPlatform],
	[kp].[CreatedLatitude],
	[kp].[CreatedLongitude],
    [kp].[LastModifiedBy],
    [lm].[Name] AS LastModifiedByUserName,
    [kp].[LastModifiedAt],
    [kp].[LastModifiedFormFactor],
	[kp].[LastModifiedPlatform],
	[kp].[LastModifiedLatitude],
	[kp].[LastModifiedLongitude],

    [kp].[Status]

FROM
    [dbo].[KitchenProduction] kp
INNER JOIN
    [dbo].[Company] c ON kp.CompanyId = c.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON kp.FinancialYearId = fy.Id
INNER JOIN
    [dbo].[Kitchen] k ON kp.KitchenId = k.Id
INNER JOIN
    [dbo].[User] u ON kp.CreatedBy = u.Id
LEFT JOIN
    [dbo].[User] lm ON kp.LastModifiedBy = lm.Id
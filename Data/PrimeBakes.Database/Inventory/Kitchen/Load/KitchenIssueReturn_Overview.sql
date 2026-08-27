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

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
    [kpr].[CreatedFromPlatform],
    [kpr].[LastModifiedBy],
    [lm].[Name] AS LastModifiedByUserName,
    [kpr].[LastModifiedAt],
    [kpr].[LastModifiedFromPlatform],

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

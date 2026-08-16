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
    [kpr].[CreatedFromPlatform],
    [kpr].[LastModifiedBy],
    [lm].[Name] AS LastModifiedByUserName,
    [kpr].[LastModifiedAt],
    [kpr].[LastModifiedFromPlatform],

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

WHERE
    [kprd].[Status] = 1;

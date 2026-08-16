CREATE VIEW [dbo].[KitchenProduction_Item_Overview]
AS
SELECT
    [kpd].[Id],
    [kpd].[ProductId] AS ItemId,
    [p].[Name] AS ItemName,
    [p].[Code] AS ItemCode,
    [p].[ProductCategoryId] AS ItemCategoryId,
    [pc].[Name] AS ItemCategoryName,

    [kpd].[Quantity],
    [kpd].[Rate],
    [kpd].[Total],

    [kpd].[Remarks] AS ItemRemarks,

    [kpd].[MasterId],
    [kp].[TransactionNo],
    [kp].[CompanyId],
    [c].[Name] AS CompanyName,

    [kp].[TransactionDateTime],
    [kp].[FinancialYearId],
    CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

    [kp].[KitchenId],
    [k].[Name] AS KitchenName,
    [kp].[Remarks] AS KitchenProductionRemarks,

    [kp].[TotalItems],
    [kp].[TotalQuantity],
    [kp].[TotalAmount],

    [kp].[CreatedBy],
    [u].[Name] AS CreatedByName,
    [kp].[CreatedAt],
    [kp].[CreatedFromPlatform],
    [kp].[LastModifiedBy],
    [lm].[Name] AS LastModifiedByUserName,
    [kp].[LastModifiedAt],
    [kp].[LastModifiedFromPlatform],

    [kp].[Status] AS MasterStatus

FROM
    [dbo].[KitchenProductionDetail] kpd
INNER JOIN
    [dbo].[KitchenProduction] kp ON kpd.MasterId = kp.Id
INNER JOIN
    [dbo].[Product] p ON kpd.ProductId = p.Id
INNER JOIN
    [dbo].[ProductCategory] pc ON p.ProductCategoryId = pc.Id
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

WHERE
    [kpd].[Status] = 1;
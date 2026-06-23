CREATE VIEW [dbo].[KitchenIssue_Item_Overview]
AS
SELECT
    [kid].[Id],
    [kid].[RawMaterialId] AS ItemId,
    [rm].[Name] AS ItemName,
    [rm].[Code] AS ItemCode,
    [rm].[RawMaterialCategoryId] AS ItemCategoryId,
    [rc].[Name] AS ItemCategoryName,

    [kid].[Quantity],
    [kid].[UnitOfMeasurement],
    [kid].[Rate],
    [kid].[Total],

    [kid].[Remarks] AS ItemRemarks,

    [kid].[MasterId],
    [ki].[TransactionNo],
    [ki].[CompanyId],
    [c].[Name] AS CompanyName,

    [ki].[TransactionDateTime],
    [ki].[FinancialYearId],
    CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

    [ki].[KitchenId],
    [k].[Name] AS KitchenName,
    [ki].[Remarks] AS KitchenIssueRemarks,

    [ki].[TotalItems],
    [ki].[TotalQuantity],
    [ki].[TotalAmount],

    [ki].[CreatedBy],
    [u].[Name] AS CreatedByName,
    [ki].[CreatedAt],
    [ki].[CreatedFromPlatform],
    [ki].[LastModifiedBy],
    [lm].[Name] AS LastModifiedByUserName,
    [ki].[LastModifiedAt],
    [ki].[LastModifiedFromPlatform],

    [ki].[Status] AS MasterStatus

FROM
    [dbo].[KitchenIssueDetail] kid
INNER JOIN
    [dbo].[KitchenIssue] ki ON kid.MasterId = ki.Id
INNER JOIN
    [dbo].[RawMaterial] rm ON kid.RawMaterialId = rm.Id
INNER JOIN
    [dbo].[RawMaterialCategory] rc ON rm.RawMaterialCategoryId = rc.Id
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

WHERE
    [kid].[Status] = 1;
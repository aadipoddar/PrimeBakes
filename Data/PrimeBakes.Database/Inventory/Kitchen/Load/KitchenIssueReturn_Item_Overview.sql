CREATE VIEW [dbo].[KitchenIssueReturn_Item_Overview]
AS
SELECT
    [kird].[Id],
    [kird].[RawMaterialId] AS ItemId,
    [rm].[Name] AS ItemName,
    [rm].[Code] AS ItemCode,
    [rm].[RawMaterialCategoryId] AS ItemCategoryId,
    [rc].[Name] AS ItemCategoryName,

    [kird].[Quantity],
    [kird].[UnitOfMeasurement],
    [kird].[Rate],
    [kird].[Total],

    [kird].[Remarks] AS ItemRemarks,

    [kird].[MasterId],
    [kir].[TransactionNo],
    [kir].[CompanyId],
    [c].[Name] AS CompanyName,

    [kir].[TransactionDateTime],
    [kir].[FinancialYearId],
    CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

    [kir].[KitchenId],
    [k].[Name] AS KitchenName,
    [kir].[Remarks] AS KitchenIssueReturnRemarks,

    [kir].[TotalItems],
    [kir].[TotalQuantity],
    [kir].[TotalAmount],

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

    [kir].[Status] AS MasterStatus

FROM
    [dbo].[KitchenIssueReturnDetail] kird
INNER JOIN
    [dbo].[KitchenIssueReturn] kir ON kird.MasterId = kir.Id
INNER JOIN
    [dbo].[RawMaterial] rm ON kird.RawMaterialId = rm.Id
INNER JOIN
    [dbo].[RawMaterialCategory] rc ON rm.RawMaterialCategoryId = rc.Id
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

WHERE
    [kird].[Status] = 1;

CREATE VIEW [dbo].[PurchaseOrder_Item_Overview]
	AS
SELECT
	[pod].[Id],
	[pod].[RawMaterialId] AS ItemId,
	[rm].[Name] AS ItemName,
	[rm].[Code] AS ItemCode,
	[rm].[RawMaterialCategoryId] AS ItemCategoryId,
	[rc].[Name] AS ItemCategoryName,

	[pod].[Quantity],
	[pod].[UnitOfMeasurement],
	[pod].[Remarks] AS ItemRemarks,

	[pod].[MasterId],
	[po].[TransactionNo],
	[po].[CompanyId],
	[c].[Name] AS CompanyName,
	[po].[PartyId],
	[l].[Name] AS PartyName,

	[po].[PurchaseId],
	[p].[TransactionNo] AS PurchaseTransactionNo,
	[p].[TransactionDateTime] AS PurchaseDateTime,

	[po].[TransactionDateTime],
	[po].[ExpectedDeliveryDate],
	[po].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

	[po].[TotalItems],
	[po].[TotalQuantity],

	[po].[Remarks],
	[po].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[po].[CreatedAt],
	[po].[CreatedFormFactor],
	[po].[CreatedPlatform],
	[po].[CreatedLatitude],
	[po].[CreatedLongitude],
	[po].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[po].[LastModifiedAt],
	[po].[LastModifiedFormFactor],
	[po].[LastModifiedPlatform],
	[po].[LastModifiedLatitude],
	[po].[LastModifiedLongitude],

	CASE WHEN [po].[CreatedLatitude] IS NOT NULL AND [ul].[Latitude] IS NOT NULL THEN geography::Point([po].[CreatedLatitude], [po].[CreatedLongitude], 4326).STDistance(geography::Point([ul].[Latitude], [ul].[Longitude], 4326)) END AS CreatedUserOffset,
	CASE WHEN [po].[LastModifiedLatitude] IS NOT NULL AND [lml].[Latitude] IS NOT NULL THEN geography::Point([po].[LastModifiedLatitude], [po].[LastModifiedLongitude], 4326).STDistance(geography::Point([lml].[Latitude], [lml].[Longitude], 4326)) END AS LastModifiedUserOffset,

	[po].[Status] AS MasterStatus

FROM
	[dbo].[PurchaseOrderDetail] pod

INNER JOIN
	[dbo].[PurchaseOrder] po ON pod.[MasterId] = po.Id
INNER JOIN
	[dbo].[RawMaterial] rm ON pod.RawMaterialId = rm.Id
INNER JOIN
	[dbo].[RawMaterialCategory] rc ON rm.RawMaterialCategoryId = rc.Id
INNER JOIN
	[dbo].[Company] c ON po.CompanyId = c.Id
INNER JOIN
	[dbo].[Ledger] l ON po.PartyId = l.Id
LEFT JOIN
	[dbo].[Purchase] p ON po.PurchaseId = p.Id
INNER JOIN
	[dbo].[FinancialYear] AS fy ON po.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[User] AS u ON po.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON po.LastModifiedBy = lm.Id
LEFT JOIN
	[dbo].[Location] AS ul ON u.LocationId = ul.Id
LEFT JOIN
	[dbo].[Location] AS lml ON lm.LocationId = lml.Id

WHERE
	[pod].[Status] = 1;

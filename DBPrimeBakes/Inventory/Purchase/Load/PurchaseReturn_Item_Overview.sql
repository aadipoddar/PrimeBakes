CREATE VIEW [dbo].[PurchaseReturn_Item_Overview]
	AS
SELECT
	[pd].[Id],
	[pd].[RawMaterialId] AS ItemId,
	[rm].[Name] AS ItemName,
	[rm].[Code] AS ItemCode,
	[rm].[RawMaterialCategoryId] AS ItemCategoryId,
	[rc].[Name] AS ItemCategoryName,

	[pd].[Quantity],
	[pd].[UnitOfMeasurement],
	[pd].[Rate],
	[pd].[BaseTotal] AS ItemBaseTotal,

	[pd].[DiscountPercent],
	[pd].[DiscountAmount],
	[pd].[AfterDiscount],

	[pd].[CGSTPercent],
	[pd].[CGSTAmount],
	[pd].[SGSTPercent],
	[pd].[SGSTAmount],
	[pd].[IGSTPercent],
	[pd].[IGSTAmount],
	[pd].[TotalTaxAmount],
	[pd].[InclusiveTax],

	[pd].[Total],
	[pd].[NetRate],
	[pd].[NetRate] * [pd].[Quantity] AS NetTotal,

	[pd].[Remarks] AS ItemRemarks,

	[pd].[MasterId],
	[p].[TransactionNo],
    [p].[CompanyId],
    [c].[Name] AS CompanyName,

    [p].[TransactionDateTime],
    [p].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

	[p].[ChallanNo],
	[p].[PartyId],
	[l].[Name] AS PartyName,

	[p].[TotalItems],
	[p].[TotalQuantity],
	[p].[BaseTotal],
	[p].[ItemDiscountAmount],
	[p].[TotalAfterItemDiscount],
	[p].[TotalInclusiveTaxAmount],
	[p].[TotalExtraTaxAmount],
	[p].[TotalAfterTax],

	[p].[OtherChargesPercent],
	[p].[OtherChargesAmount],
	[p].[CashDiscountPercent],
	[p].[CashDiscountAmount],

	[p].[RoundOffAmount],
	[p].[TotalAmount],

    [p].[Remarks],
	[p].[DocumentUrl],
	[p].[FinancialAccountingId],
	[fa].[TransactionNo] AS FinancialAccountingTransactionNo,
	[p].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[p].[CreatedAt],
	[p].[CreatedFromPlatform],
	[p].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[p].[LastModifiedAt],
	[p].[LastModifiedFromPlatform],

	[p].[Status] AS MasterStatus

FROM
	[dbo].[PurchaseReturnDetail] pd
INNER JOIN
	[dbo].[PurchaseReturn] p ON pd.MasterId = p.Id
INNER JOIN
	[dbo].[RawMaterial] rm ON pd.RawMaterialId = rm.Id
INNER JOIN
	[dbo].[RawMaterialCategory] rc ON rm.RawMaterialCategoryId = rc.Id
INNER JOIN
    [dbo].[Company] c ON p.CompanyId = c.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON p.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[Ledger] l ON p.PartyId = l.Id
LEFT JOIN
	[dbo].[FinancialAccounting] fa ON p.FinancialAccountingId = fa.Id
INNER JOIN
	[dbo].[User] AS u ON p.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON p.LastModifiedBy = lm.Id

WHERE
	[pd].[Status] = 1;
CREATE VIEW [dbo].[FinancialAccounting_Ledger_Overview]
	AS
SELECT
	[ad].[Id],
	[ad].[LedgerId],
	[l].[Name] AS LedgerName,
	[l].[Code] AS LedgerCode,

	[l].[AccountTypeId],
	[at].[Name] AS AccountTypeName,
	[l].[GroupId],
	[g].[Name] AS GroupName,

	[ad].[ReferenceId] AS LedgerReferenceId,
	[ad].[ReferenceType] AS LedgerReferenceType,
	[ad].[ReferenceNo] AS LedgerReferenceNo,

	(CASE
		WHEN [ad].[ReferenceType] = 'Sale' THEN
			(SELECT TransactionDateTime FROM [dbo].[Sale_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'SaleReturn' THEN
			(SELECT TransactionDateTime FROM [dbo].[SaleReturn_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'Purchase' THEN
			(SELECT TransactionDateTime FROM [dbo].[Purchase_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'PurchaseReturn' THEN
			(SELECT TransactionDateTime FROM [dbo].[PurchaseReturn_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'StockTransfer' THEN
			(SELECT TransactionDateTime FROM [dbo].[StockTransfer_Overview] WHERE Id = [ad].[ReferenceId])
	END) AS ReferenceDateTime,

	(CASE
		WHEN [ad].[ReferenceType] = 'Sale' THEN
			(SELECT TotalAmount FROM [dbo].[Sale_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'SaleReturn' THEN
			(SELECT TotalAmount FROM [dbo].[SaleReturn_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'Purchase' THEN
			(SELECT TotalAmount FROM [dbo].[Purchase_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'PurchaseReturn' THEN
			(SELECT TotalAmount FROM [dbo].[PurchaseReturn_Overview] WHERE Id = [ad].[ReferenceId])
		WHEN [ad].[ReferenceType] = 'StockTransfer' THEN
			(SELECT TotalAmount FROM [dbo].[StockTransfer_Overview] WHERE Id = [ad].[ReferenceId])
	END) AS ReferenceAmount,

	[ad].[Debit] AS Debit,
	[ad].[Credit] AS Credit,

	[ad].[Remarks] AS LedgerRemarks,

	[ad].[MasterId],
    [a].[TransactionNo],
    [a].[CompanyId],
    [c].[Name] AS CompanyName,
    [a].[VoucherId],
    [v].[Name] AS VoucherName,

    [a].[ReferenceId],
    [a].[ReferenceNo],

    [a].[TransactionDateTime],
    [a].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS FinancialYear,

    [a].[TotalCreditLedgers] + [a].[TotalDebitLedgers] AS TotalLedgers,

    [a].[TotalCreditLedgers],
    [a].[TotalDebitLedgers],
    [a].[TotalDebitAmount],
    [a].[TotalCreditAmount],

    [a].[TotalDebitAmount] + [a].[TotalCreditAmount] AS TotalAmount,

    [a].[Remarks],
	[a].[CreatedBy],
	[u].[Name] AS CreatedByName,
	[a].[CreatedAt],
	[a].[CreatedFromPlatform],
	[a].[LastModifiedBy],
	[lm].[Name] AS LastModifiedByUserName,
	[a].[LastModifiedAt],
	[a].[LastModifiedFromPlatform],

	[a].[Status]

FROM
	[dbo].[FinancialAccountingLedger] ad

INNER JOIN
	[dbo].[FinancialAccounting] a ON ad.[MasterId] = a.Id
INNER JOIN
	[dbo].[Ledger] l ON ad.LedgerId = l.Id
INNER JOIN
	[dbo].[AccountType] at ON l.AccountTypeId = at.Id
INNER JOIN
	[dbo].[Group] g ON l.GroupId = g.Id
INNER JOIN
    [dbo].[Company] c ON a.CompanyId = c.Id
INNER JOIN
    [dbo].[Voucher] v ON a.VoucherId = v.Id
INNER JOIN
    [dbo].[FinancialYear] fy ON a.FinancialYearId = fy.Id
INNER JOIN
	[dbo].[User] AS u ON a.CreatedBy = u.Id
LEFT JOIN
	[dbo].[User] AS lm ON a.LastModifiedBy = lm.Id

WHERE
	[ad].[Status] = 1;
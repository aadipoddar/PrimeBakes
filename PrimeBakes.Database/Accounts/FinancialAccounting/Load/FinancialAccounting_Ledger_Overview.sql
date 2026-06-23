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

	[ad].[Debit] AS Debit,
	[ad].[Credit] AS Credit,

	[ad].[InstrumentNo] AS InstrumentNo,
	[ad].[InstrumentDate] AS InstrumentDate,
	[ad].[ClearingDate] AS ClearingDate,

	CASE
		WHEN [ad].[InstrumentDate] IS NULL THEN NULL
		WHEN [ad].[ClearingDate] IS NOT NULL THEN CAST(1 AS BIT)
		ELSE CAST(0 AS BIT)
	END AS Reconciled,

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

	[a].[Status] AS MasterStatus

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
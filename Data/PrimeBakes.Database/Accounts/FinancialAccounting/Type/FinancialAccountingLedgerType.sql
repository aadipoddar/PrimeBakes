CREATE TYPE [dbo].[FinancialAccountingLedgerType] AS TABLE
(
	[Id] INT NOT NULL,
	[MasterId] INT NOT NULL,
	[LedgerId] INT NOT NULL,
	[ReferenceId] INT NULL,
	[ReferenceType] VARCHAR(MAX) NULL,
	[ReferenceNo] VARCHAR(MAX) NULL,
	[Debit] MONEY NULL,
	[Credit] MONEY NULL,
	[InstrumentNo] VARCHAR(MAX) NULL,
	[InstrumentDate] DATETIME NULL,
	[ClearingDate] DATETIME NULL,
	[Remarks] VARCHAR(MAX) NULL,
	[Status] BIT NOT NULL
)
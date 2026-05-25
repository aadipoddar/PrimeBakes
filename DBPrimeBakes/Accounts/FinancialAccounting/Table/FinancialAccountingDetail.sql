CREATE TABLE [dbo].[FinancialAccountingDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [MasterId] INT NOT NULL,
    [LedgerId] INT NOT NULL, 
    [ReferenceType] VARCHAR(MAX) NULL, 
    [ReferenceId] INT NULL, 
    [ReferenceNo] VARCHAR(MAX) NULL,
    [Debit] MONEY NULL, 
    [Credit] MONEY NULL, 
    [Remarks] VARCHAR(MAX) NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_FinancialAccountingDetails_ToAccounting] FOREIGN KEY ([MasterId]) REFERENCES [FinancialAccounting](Id), 
    CONSTRAINT [FK_FinancialAccountingDetails_ToLedger] FOREIGN KEY (LedgerId) REFERENCES [Ledger](Id)

)

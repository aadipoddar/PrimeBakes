CREATE TABLE [dbo].[PurchaseOrder]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[TransactionNo] VARCHAR(100) NOT NULL UNIQUE,
	[CompanyId] INT NOT NULL,
	[PartyId] INT NOT NULL,
	[PurchaseId] INT NULL,
	[TransactionDateTime] DATETIME NOT NULL,
	[ExpectedDeliveryDate] DATE NULL,
	[FinancialYearId] INT NOT NULL,
	[TotalItems] INT NOT NULL DEFAULT 0,
	[TotalQuantity] MONEY NOT NULL DEFAULT 0,
	[Remarks] VARCHAR(MAX) NULL,
	[Status] BIT NOT NULL DEFAULT 1,

	[CreatedBy] INT NOT NULL,
	[CreatedAt] DATETIME NOT NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')),
	[CreatedFormFactor] VARCHAR(MAX) NULL,
	[CreatedPlatform] VARCHAR(MAX) NULL,
	[CreatedLatitude] DECIMAL(9,6) NULL,
	[CreatedLongitude] DECIMAL(9,6) NULL,

	[LastModifiedBy] INT NULL,
	[LastModifiedAt] DATETIME NULL,
	[LastModifiedFormFactor] VARCHAR(MAX) NULL,
	[LastModifiedPlatform] VARCHAR(MAX) NULL,
	[LastModifiedLatitude] DECIMAL(9,6) NULL,
	[LastModifiedLongitude] DECIMAL(9,6) NULL,

    CONSTRAINT [FK_PurchaseOrder_ToCompany] FOREIGN KEY ([CompanyId]) REFERENCES [Company]([Id]),
    CONSTRAINT [FK_PurchaseOrder_ToLedger] FOREIGN KEY ([PartyId]) REFERENCES [Ledger]([Id]),
    CONSTRAINT [FK_PurchaseOrder_ToPurchase] FOREIGN KEY ([PurchaseId]) REFERENCES [Purchase]([Id]),
    CONSTRAINT [FK_PurchaseOrder_ToFinancialYear] FOREIGN KEY ([FinancialYearId]) REFERENCES [dbo].[FinancialYear]([Id]),
    CONSTRAINT [FK_PurchaseOrder_ToUser] FOREIGN KEY ([CreatedBy]) REFERENCES [User]([Id]),
	CONSTRAINT [FK_PurchaseOrder_LastModifiedBy_ToUser] FOREIGN KEY ([LastModifiedBy]) REFERENCES [User]([Id])
)
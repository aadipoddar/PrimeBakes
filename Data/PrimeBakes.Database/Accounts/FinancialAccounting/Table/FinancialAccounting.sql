CREATE TABLE [dbo].[FinancialAccounting]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[TransactionNo] VARCHAR(100) NOT NULL UNIQUE,

	[CompanyId] INT NOT NULL,
	[VoucherId] INT NOT NULL,
	[FinancialYearId] INT NOT NULL,
	[ReferenceId] INT NULL,
	[ReferenceNo] VARCHAR(MAX) NULL,

	[TransactionDateTime] DATETIME NOT NULL,
	[TotalDebitLedgers] INT NOT NULL DEFAULT 0,
	[TotalCreditLedgers] INT NOT NULL DEFAULT 0,
	[TotalDebitAmount] MONEY NOT NULL DEFAULT 0,
	[TotalCreditAmount] MONEY NOT NULL DEFAULT 0,
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

	CONSTRAINT [FK_FinancialAccounting_ToCompany] FOREIGN KEY ([CompanyId]) REFERENCES [Company]([Id]),
	CONSTRAINT [FK_FinancialAccounting_ToVoucher] FOREIGN KEY ([VoucherId]) REFERENCES [Voucher]([Id]), 
	CONSTRAINT [FK_FinancialAccounting_ToFinancialYear] FOREIGN KEY ([FinancialYearId]) REFERENCES [FinancialYear]([Id]), 
	CONSTRAINT [FK_FinancialAccounting_CreatedBy_ToUser] FOREIGN KEY ([CreatedBy]) REFERENCES [User]([Id]),
	CONSTRAINT [FK_FinancialAccounting_LastModifiedBy_ToUser] FOREIGN KEY ([LastModifiedBy]) REFERENCES [User]([Id])
)

CREATE TABLE [dbo].[AuditTrail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[Action] VARCHAR(MAX) NOT NULL,
	[TableName] VARCHAR(MAX) NOT NULL,
	[RecordNo] VARCHAR(MAX) NOT NULL,
	[RecordValue] VARCHAR(MAX) NULL,
	[CreatedBy] INT NOT NULL,
	[CreatedByName] VARCHAR(MAX) NOT NULL,
	[TransactionDateTime] DATETIME NOT NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')),
	[CreatedFromPlatform] VARCHAR(MAX) NOT NULL,
	CONSTRAINT [FK_AuditTrail_CreatedBy_ToUser] FOREIGN KEY ([CreatedBy]) REFERENCES [User]([Id]),
)

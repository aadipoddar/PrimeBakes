CREATE TABLE [dbo].[AuditTrail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[Action] VARCHAR(20) NOT NULL,
	[TableName] VARCHAR(100) NOT NULL,
	[RecordNo] VARCHAR(500) NOT NULL,
	[RecordValue] VARCHAR(MAX) NULL,
	[CreatedBy] INT NOT NULL,
	[CreatedByName] VARCHAR(MAX) NOT NULL,
	[TransactionDateTime] DATETIME NOT NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')),
	[CreatedFormFactor] VARCHAR(MAX) NULL,
	[CreatedPlatform] VARCHAR(MAX) NULL,
	[CreatedLatitude] DECIMAL(9,6) NULL,
	[CreatedLongitude] DECIMAL(9,6) NULL,
	CONSTRAINT [FK_AuditTrail_CreatedBy_ToUser] FOREIGN KEY ([CreatedBy]) REFERENCES [User]([Id]),
	INDEX [IX_AuditTrail_TableName_RecordNo] NONCLUSTERED ([TableName], [RecordNo], [Id] DESC) WHERE [Action] <> 'Report'
)

CREATE TABLE [dbo].[PulpTrace]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
	[TransactionDateTime] DATETIME NOT NULL,
	[MachineName] VARCHAR(MAX) NULL,
	[ReelNo] VARCHAR(MAX) NOT NULL,
	[DeckleCM] INT NULL,
	[GSM] INT NULL,
	[DF] INT NULL,
	[PaperShade] VARCHAR(MAX) NULL,
	[ActualWeight] INT NULL,
	[UsedWeight] INT NULL,
	[BalanceWeight] INT NULL,
	[Scanned] BIT NOT NULL DEFAULT 0, 
)

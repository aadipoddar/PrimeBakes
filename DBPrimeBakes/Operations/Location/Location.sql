CREATE TABLE [dbo].[Location]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(500) NOT NULL UNIQUE, 
    [Code] VARCHAR(10) NOT NULL UNIQUE,
    [Discount] DECIMAL(5, 2) NOT NULL DEFAULT 0,
    [LedgerId] INT NOT NULL, 
    [Remarks] VARCHAR(MAX) NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Location_ToLedger] FOREIGN KEY ([LedgerId]) REFERENCES [Ledger]([Id])
)

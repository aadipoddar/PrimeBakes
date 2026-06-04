CREATE TABLE [dbo].[DiningTable]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(500) NOT NULL UNIQUE, 
    [DiningAreaId] INT NOT NULL,
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_DiningTable_ToDiningArea] FOREIGN KEY ([DiningAreaId]) REFERENCES [DiningArea]([Id])
)

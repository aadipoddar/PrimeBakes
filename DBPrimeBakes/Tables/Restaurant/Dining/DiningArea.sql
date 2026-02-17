CREATE TABLE [dbo].[DiningArea]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(500) NOT NULL UNIQUE, 
    [LocationId] INT NOT NULL, 
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_DiningArea_ToLocation] FOREIGN KEY ([LocationId]) REFERENCES [Location]([Id])
)

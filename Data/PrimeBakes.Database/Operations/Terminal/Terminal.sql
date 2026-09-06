CREATE TABLE [dbo].[Terminal]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [TerminalNo] INT NOT NULL UNIQUE, 
    [MachineId] VARCHAR(100) NOT NULL UNIQUE, 
    [MachineName] VARCHAR(MAX) NULL, 
    [UserId] INT NOT NULL, 
    [LastSyncedAt] DATETIME NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Terminal_ToUser] FOREIGN KEY ([UserId]) REFERENCES [User]([Id])
)
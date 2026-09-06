CREATE TABLE [dbo].[Terminal]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [TerminalNo] INT NOT NULL UNIQUE, 
    [MachineId] VARCHAR(100) NOT NULL UNIQUE, 
    [MachineName] VARCHAR(MAX) NOT NULL, 
    [UserId] INT NOT NULL, 
    [LastSyncedAt] DATETIME NULL, 
    CONSTRAINT [FK_Terminal_ToUser] FOREIGN KEY ([UserId]) REFERENCES [User]([Id])
)
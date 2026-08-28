CREATE TABLE [dbo].[User]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(500) NOT NULL UNIQUE, 
    [Passcode] SMALLINT NOT NULL UNIQUE, 
    [LocationId] INT NOT NULL, 
    [ChangeProductFinancial] BIT NOT NULL DEFAULT 0,
    [Accounts] BIT NOT NULL DEFAULT 0, 
    [Inventory] BIT NOT NULL DEFAULT 0, 
    [Store] BIT NOT NULL DEFAULT 0, 
    [Restaurant] BIT NOT NULL DEFAULT 0, 
    [Payroll] BIT NOT NULL DEFAULT 0, 
    [Reports] BIT NOT NULL DEFAULT 0, 
    [Admin] BIT NOT NULL DEFAULT 0, 
    [Remarks] VARCHAR(MAX) NULL,
    [LastLoginTime] DATETIME NULL,
    [LastSeen] DATETIME NULL,
    [LastSeenFormFactor] VARCHAR(MAX) NULL,
    [LastSeenPlatform] VARCHAR(MAX) NULL,
    [LastSeenLatitude] DECIMAL(9,6) NULL,
    [LastSeenLongitude] DECIMAL(9,6) NULL,
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Users_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id)
)

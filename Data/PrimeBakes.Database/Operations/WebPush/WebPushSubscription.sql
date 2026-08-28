CREATE TABLE [dbo].[WebPushSubscription]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[UserId] INT NOT NULL,
	[Endpoint] VARCHAR(500) NOT NULL,
	[P256dh] VARCHAR(200) NOT NULL,
	[Auth] VARCHAR(100) NOT NULL,
	[TransactionDateTime] DATETIME NOT NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')),
	CONSTRAINT [FK_WebPushSubscription_UserId_ToUser] FOREIGN KEY ([UserId]) REFERENCES [User]([Id]),
	CONSTRAINT [UQ_WebPushSubscription_Endpoint] UNIQUE ([Endpoint]),
	INDEX [IX_WebPushSubscription_UserId] NONCLUSTERED ([UserId])
)

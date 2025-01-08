CREATE TABLE [dbo].[Order] (
    [Id]         INT      IDENTITY (1, 1) NOT NULL,
    [UserId]     INT      NOT NULL,
    [CustomerId] INT      NOT NULL,
    [DateTime]   DATETIME DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')) NOT NULL,
    [Status]     BIT      DEFAULT ((1)) NOT NULL,
    CONSTRAINT [PK_Order] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Order_ToCustomer] FOREIGN KEY ([CustomerId]) REFERENCES [dbo].[Customer] ([Id]),
    CONSTRAINT [FK_Order_ToUser] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([Id])
);


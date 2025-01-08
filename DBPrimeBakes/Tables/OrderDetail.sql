CREATE TABLE [dbo].[OrderDetail] (
    [Id]       INT IDENTITY (1, 1) NOT NULL,
    [OrderId]  INT NOT NULL,
    [ItemId]   INT NOT NULL,
    [Quantity] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_OrderDetail_ToItem] FOREIGN KEY ([ItemId]) REFERENCES [dbo].[Item] ([Id]),
    CONSTRAINT [FK_OrderDetail_ToOrder] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Order] ([Id])
);


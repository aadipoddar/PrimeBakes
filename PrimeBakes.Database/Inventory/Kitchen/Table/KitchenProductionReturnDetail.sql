CREATE TABLE [dbo].[KitchenProductionReturnDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [MasterId] INT NOT NULL,
    [ProductId] INT NOT NULL,
	[Quantity] MONEY NOT NULL DEFAULT 1,
	[Rate] MONEY NOT NULL,
    [Total] MONEY NOT NULL DEFAULT 0,
    [Remarks] VARCHAR(MAX) NULL,
	[Status] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_KitchenProductionReturnDetail_ToKitchenProductionReturn] FOREIGN KEY ([MasterId]) REFERENCES [KitchenProductionReturn](Id),
    CONSTRAINT [FK_KitchenProductionReturnDetail_ToProduct] FOREIGN KEY (ProductId) REFERENCES [Product](Id)
)

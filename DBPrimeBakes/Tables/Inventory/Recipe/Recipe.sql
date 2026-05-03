CREATE TABLE [dbo].[Recipe]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProductId] INT NOT NULL,
    [Quantity] MONEY NOT NULL DEFAULT 1,
    [Deduct] BIT NOT NULL DEFAULT 1,
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_Recipe_ToProduct] FOREIGN KEY (ProductId) REFERENCES [Product](Id)
)

CREATE TABLE [dbo].[ProductStock]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProductId] INT NOT NULL ,
    [Quantity] MONEY NOT NULL,
    [NetRate] MONEY NOT NULL, 
    [Type] VARCHAR(20) NOT NULL, 
    [TransactionId] INT NULL, 
    [TransactionNo] VARCHAR(100) NOT NULL, 
    [TransactionDateTime] DATETIME NOT NULL,  
    [LocationId] INT NOT NULL, 
    CONSTRAINT [FK_ProductStock_ToProduct] FOREIGN KEY (ProductId) REFERENCES [Product](Id), 
    CONSTRAINT [FK_ProductStock_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id),
    INDEX [IX_ProductStock_TransactionNo] NONCLUSTERED ([TransactionNo]),
    INDEX [IX_ProductStock_Location_TransactionDateTime] NONCLUSTERED ([LocationId], [TransactionDateTime], [ProductId], [Quantity])
)

CREATE TABLE [dbo].[RawMaterialStock]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [RawMaterialId] INT NOT NULL ,
    [Quantity] MONEY NOT NULL,
    [NetRate] MONEY NOT NULL, 
    [Type] VARCHAR(20) NOT NULL, 
    [TransactionId] INT NULL, 
    [TransactionNo] VARCHAR(100) NOT NULL, 
    [TransactionDateTime] DATETIME NOT NULL, 
    CONSTRAINT [FK_RawMaterialStock_ToRawMaterial] FOREIGN KEY (RawMaterialId) REFERENCES [RawMaterial](Id),
    INDEX [IX_RawMaterialStock_TransactionNo] NONCLUSTERED ([TransactionNo]),
    INDEX [IX_RawMaterialStock_TransactionDateTime] NONCLUSTERED ([TransactionDateTime], [RawMaterialId], [Quantity])
)

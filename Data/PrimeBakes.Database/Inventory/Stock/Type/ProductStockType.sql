CREATE TYPE [dbo].[ProductStockType] AS TABLE
(
	[Id] INT NOT NULL,
	[ProductId] INT NOT NULL,
	[Quantity] MONEY NOT NULL,
	[NetRate] MONEY NOT NULL,
	[Type] VARCHAR(20) NOT NULL,
	[TransactionId] INT NULL,
	[TransactionNo] VARCHAR(100) NOT NULL,
	[TransactionDateTime] DATETIME NOT NULL,
	[LocationId] INT NOT NULL
)
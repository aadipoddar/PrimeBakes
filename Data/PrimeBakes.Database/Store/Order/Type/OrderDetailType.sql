CREATE TYPE [dbo].[OrderDetailType] AS TABLE
(
	[Id] INT NOT NULL,
	[MasterId] INT NOT NULL,
	[ProductId] INT NOT NULL,
	[Quantity] MONEY NOT NULL,
	[Remarks] VARCHAR(MAX) NULL,
	[Status] BIT NOT NULL
)

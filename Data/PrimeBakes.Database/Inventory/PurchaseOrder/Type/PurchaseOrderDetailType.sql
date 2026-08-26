CREATE TYPE [dbo].[PurchaseOrderDetailType] AS TABLE
(
	[Id] INT NOT NULL,
	[MasterId] INT NOT NULL,
	[RawMaterialId] INT NOT NULL,
	[Quantity] MONEY NOT NULL,
	[UnitOfMeasurement] VARCHAR(20) NOT NULL,
	[Remarks] VARCHAR(MAX) NULL,
	[Status] BIT NOT NULL
)
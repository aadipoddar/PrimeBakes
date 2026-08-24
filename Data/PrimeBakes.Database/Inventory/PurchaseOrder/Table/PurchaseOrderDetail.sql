CREATE TABLE [dbo].[PurchaseOrderDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [MasterId] INT NOT NULL,
	[RawMaterialId] INT NOT NULL,
	[Quantity] MONEY NOT NULL DEFAULT 1,
	[UnitOfMeasurement] VARCHAR(20) NOT NULL,
	[Remarks] VARCHAR(MAX) NULL,
	[Status] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_PurchaseOrderDetail_ToPurchaseOrder] FOREIGN KEY ([MasterId]) REFERENCES [PurchaseOrder]([Id]),
	CONSTRAINT [FK_PurchaseOrderDetail_ToRawMaterial] FOREIGN KEY ([RawMaterialId]) REFERENCES [RawMaterial]([Id])
)
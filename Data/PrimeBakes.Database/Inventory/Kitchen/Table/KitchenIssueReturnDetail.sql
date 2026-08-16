CREATE TABLE [dbo].[KitchenIssueReturnDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [MasterId] INT NOT NULL,
    [RawMaterialId] INT NOT NULL,
	[Quantity] MONEY NOT NULL DEFAULT 1,
	[UnitOfMeasurement] VARCHAR(20) NOT NULL,
	[Rate] MONEY NOT NULL,
    [Total] MONEY NOT NULL DEFAULT 0,
    [Remarks] VARCHAR(MAX) NULL,
	[Status] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_KitchenIssueReturnDetail_ToKitchenIssueReturn] FOREIGN KEY ([MasterId]) REFERENCES [KitchenIssueReturn](Id),
    CONSTRAINT [FK_KitchenIssueReturnDetail_ToRawMaterial] FOREIGN KEY (RawMaterialId) REFERENCES [RawMaterial](Id)
)

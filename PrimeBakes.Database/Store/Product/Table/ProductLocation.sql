CREATE TABLE [dbo].[ProductLocation]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [ProductId] INT NOT NULL, 
    [Rate] MONEY NOT NULL, 
    [LocationId] INT NOT NULL, 
    -- TODO - Remove Default
    [FromDate] DATE NOT NULL DEFAULT ('2025-01-01'),
    CONSTRAINT [FK_LocationProduct_ToProduct] FOREIGN KEY (ProductId) REFERENCES [Product](Id), 
    CONSTRAINT [FK_LocationProduct_ToLocation] FOREIGN KEY (LocationId) REFERENCES [Location](Id)
)

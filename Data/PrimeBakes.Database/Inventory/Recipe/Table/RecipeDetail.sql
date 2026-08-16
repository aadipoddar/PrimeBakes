CREATE TABLE [dbo].[RecipeDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [MasterId] INT NOT NULL, 
    [RawMaterialId] INT NOT NULL, 
    [Quantity] DECIMAL(7, 3) NOT NULL, 
    [Status] BIT NOT NULL DEFAULT 1, 
    CONSTRAINT [FK_RecipeDetail_ToRecipe] FOREIGN KEY ([MasterId]) REFERENCES [Recipe](Id), 
    CONSTRAINT [FK_RecipeDetail_ToRawMaterial] FOREIGN KEY (RawMaterialId) REFERENCES [RawMaterial](Id)
)

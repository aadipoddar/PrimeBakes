CREATE TABLE [dbo].[Item] (
    [Id]         INT           IDENTITY (1, 1) NOT NULL,
    [ItemCategoryId] INT           NOT NULL,
    [Code]       VARCHAR (100) NOT NULL,
    [Name]       VARCHAR (100) NOT NULL,
    [UserCategoryId] INT NOT NULL, 
    [Status]     BIT           DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Item_ToCategory] FOREIGN KEY ([ItemCategoryId]) REFERENCES [dbo].[ItemCategory] ([Id]), 
    CONSTRAINT [FK_Item_ToTable] FOREIGN KEY (UserCategoryId) REFERENCES UserCategory(Id)
);


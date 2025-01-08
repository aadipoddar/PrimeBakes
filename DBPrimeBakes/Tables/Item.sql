CREATE TABLE [dbo].[Item] (
    [Id]         INT           IDENTITY (1, 1) NOT NULL,
    [CategoryId] INT           NOT NULL,
    [Code]       VARCHAR (100) NOT NULL,
    [Name]       VARCHAR (100) NOT NULL,
    [Status]     BIT           DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Item_ToCategory] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Category] ([Id])
);


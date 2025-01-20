CREATE TABLE [dbo].[Customer] (
    [Id]     INT           IDENTITY (1, 1) NOT NULL,
    [Code]   VARCHAR (100) NOT NULL,
    [Name]   VARCHAR (100) NOT NULL,
    [Email] VARCHAR(100) NOT NULL, 
    [Status] BIT           DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);


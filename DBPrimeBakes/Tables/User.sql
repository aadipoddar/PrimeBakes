CREATE TABLE [dbo].[User] (
    [Id]       INT           IDENTITY (1, 1) NOT NULL,
    [Name]     VARCHAR (100) NOT NULL,
    [Code] VARCHAR(100) NOT NULL, 
    [Password] VARCHAR (100) NOT NULL,
    [CustomerId] INT NOT NULL, 
    [Status]   BIT           DEFAULT ((1)) NOT NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC), 
    CONSTRAINT [FK_User_ToCustomer] FOREIGN KEY (CustomerId) REFERENCES [Customer]([Id])
);


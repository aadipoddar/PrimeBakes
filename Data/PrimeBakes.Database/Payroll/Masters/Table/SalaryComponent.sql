CREATE TABLE [dbo].[SalaryComponent]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Name] VARCHAR(250) NOT NULL UNIQUE, 
    [Code] VARCHAR(50) NOT NULL UNIQUE, 
    [ComponentType] VARCHAR(30) NOT NULL, 
    [Formula] VARCHAR(MAX) NULL, 
    [Sequence] INT NOT NULL, 
    [Prorate] BIT NOT NULL DEFAULT 0, 
    [Rounding] BIT NOT NULL DEFAULT 1, 
    [ShowOnPayslip] BIT NOT NULL DEFAULT 1, 
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1
)

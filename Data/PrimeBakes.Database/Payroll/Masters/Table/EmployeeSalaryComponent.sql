CREATE TABLE [dbo].[EmployeeSalaryComponent]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [EmployeeId] INT NOT NULL, 
    [SalaryComponentId] INT NOT NULL, 
    [Amount] MONEY NOT NULL DEFAULT 0, 
    [Formula] VARCHAR(MAX) NULL, 
    [Prorate] BIT NOT NULL DEFAULT 0, 
    [FromDate] DATE NOT NULL, 
    [Remarks] VARCHAR(MAX) NULL,
    CONSTRAINT [FK_EmployeeSalaryComponent_ToEmployee] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee]([Id]), 
    CONSTRAINT [FK_EmployeeSalaryComponent_ToSalaryComponent] FOREIGN KEY ([SalaryComponentId]) REFERENCES [SalaryComponent]([Id])
)

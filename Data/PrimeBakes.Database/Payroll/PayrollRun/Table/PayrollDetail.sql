CREATE TABLE [dbo].[PayrollDetail]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[MasterId] INT NOT NULL,
	[SalaryComponentId] INT NOT NULL,
	[Amount] MONEY NOT NULL DEFAULT 0,
	[Formula] VARCHAR(MAX) NULL,
	[Prorate] BIT NOT NULL DEFAULT 0,
	[Status] BIT NOT NULL DEFAULT 1,
	CONSTRAINT [FK_PayrollDetail_ToPayroll] FOREIGN KEY ([MasterId]) REFERENCES [Payroll]([Id]),
	CONSTRAINT [FK_PayrollDetail_ToSalaryComponent] FOREIGN KEY ([SalaryComponentId]) REFERENCES [SalaryComponent]([Id])
)

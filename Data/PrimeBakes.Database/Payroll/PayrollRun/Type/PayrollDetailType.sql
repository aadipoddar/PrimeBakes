CREATE TYPE [dbo].[PayrollDetailType] AS TABLE
(
	[Id] INT NOT NULL,
	[MasterId] INT NOT NULL,
	[SalaryComponentId] INT NOT NULL,
	[Amount] MONEY NOT NULL,
	[Formula] VARCHAR(MAX) NULL,
	[Prorate] BIT NOT NULL,
	[Status] BIT NOT NULL
)
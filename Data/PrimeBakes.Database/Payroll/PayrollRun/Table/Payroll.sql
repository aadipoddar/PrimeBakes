CREATE TABLE [dbo].[Payroll]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
	[TransactionNo] VARCHAR(100) NOT NULL UNIQUE,

	[EmployeeId] INT NOT NULL,
	[AttendanceId] INT NOT NULL,
	[FinancialYearId] INT NOT NULL,

	[TransactionDateTime] DATETIME NOT NULL,
	[PayrollMonth] INT NOT NULL,
	[PayrollYear] INT NOT NULL,
	[DaysInMonth] DECIMAL(5, 2) NOT NULL,
	[PaidDays] DECIMAL(5, 2) NOT NULL,
	[GrossEarnings] MONEY NOT NULL DEFAULT 0,
	[TotalDeductions] MONEY NOT NULL DEFAULT 0,
	[EmployerContribution] MONEY NOT NULL DEFAULT 0,
	[NetPay] MONEY NOT NULL DEFAULT 0,
	[Remarks] VARCHAR(MAX) NULL,

	[Status] BIT NOT NULL DEFAULT 1,

	[CreatedBy] INT NOT NULL,
	[CreatedAt] DATETIME NOT NULL DEFAULT (((getdate() AT TIME ZONE 'UTC') AT TIME ZONE 'India Standard Time')),
	[CreatedFormFactor] VARCHAR(MAX) NULL,
	[CreatedPlatform] VARCHAR(MAX) NULL,
	[CreatedLatitude] DECIMAL(9,6) NULL,
	[CreatedLongitude] DECIMAL(9,6) NULL,

	[LastModifiedBy] INT NULL,
	[LastModifiedAt] DATETIME NULL,
	[LastModifiedFormFactor] VARCHAR(MAX) NULL,
	[LastModifiedPlatform] VARCHAR(MAX) NULL,
	[LastModifiedLatitude] DECIMAL(9,6) NULL,
	[LastModifiedLongitude] DECIMAL(9,6) NULL,

	CONSTRAINT [FK_Payroll_ToEmployee] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee]([Id]),
	CONSTRAINT [FK_Payroll_ToAttendance] FOREIGN KEY ([AttendanceId]) REFERENCES [Attendance]([Id]),
	CONSTRAINT [FK_Payroll_ToFinancialYear] FOREIGN KEY ([FinancialYearId]) REFERENCES [FinancialYear]([Id]),
	CONSTRAINT [FK_Payroll_ToUser] FOREIGN KEY ([CreatedBy]) REFERENCES [User]([Id]),
	CONSTRAINT [FK_Payroll_LastModifiedBy_ToUser] FOREIGN KEY ([LastModifiedBy]) REFERENCES [User]([Id])
)

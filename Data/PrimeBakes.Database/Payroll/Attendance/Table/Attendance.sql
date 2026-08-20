CREATE TABLE [dbo].[Attendance]
(
	[Id] INT NOT NULL PRIMARY KEY IDENTITY,
    [EmployeeId] INT NOT NULL,
    [AttendanceMonth] INT NOT NULL,
    [AttendanceYear] INT NOT NULL,
    [DaysInMonth] DECIMAL(5, 2) NOT NULL,
    [PresentDays] DECIMAL(5, 2) NOT NULL DEFAULT 0,
    [WeeklyOffDays] DECIMAL(5, 2) NOT NULL DEFAULT 0,
    [HolidayDays] DECIMAL(5, 2) NOT NULL DEFAULT 0,
    [PaidLeaveDays] DECIMAL(5, 2) NOT NULL DEFAULT 0,
    [UnpaidLeaveDays] DECIMAL(5, 2) NOT NULL DEFAULT 0,
    [PaidDays] DECIMAL(5, 2) NOT NULL,
    [OvertimeHours] DECIMAL(6, 2) NOT NULL DEFAULT 0,
    [Remarks] VARCHAR(MAX) NULL,
    [Status] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [FK_Attendance_ToEmployee] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee]([Id])
)

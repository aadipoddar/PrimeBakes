CREATE VIEW [dbo].[Attendance_Overview]
AS
SELECT
	[a].[Id],
	[a].[EmployeeId],
	[e].[Code] AS [EmployeeCode],
	[e].[Name] AS [EmployeeName],
	[e].[LocationId],
	[e].[DepartmentId],
	[e].[DesignationId],
	[a].[AttendanceMonth],
	[a].[AttendanceYear],
	[a].[DaysInMonth],
	[a].[PresentDays],
	[a].[WeeklyOffDays],
	[a].[HolidayDays],
	[a].[PaidLeaveDays],
	[a].[UnpaidLeaveDays],
	[a].[PaidDays],
	[a].[OvertimeHours],
	[a].[Remarks],
	[a].[Status]

FROM Attendance a

INNER JOIN Employee e ON a.EmployeeId = e.Id;

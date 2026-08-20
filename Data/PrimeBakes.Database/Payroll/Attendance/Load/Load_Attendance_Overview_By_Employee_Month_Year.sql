CREATE PROCEDURE [dbo].[Load_Attendance_Overview_By_Employee_Month_Year]
	@EmployeeId INT = NULL,
	@AttendanceMonth INT = NULL,
	@AttendanceYear INT = NULL
AS
BEGIN

	SELECT *
	FROM Attendance_Overview ao
	WHERE (@EmployeeId IS NULL OR ao.EmployeeId = @EmployeeId)
		AND (@AttendanceMonth IS NULL OR ao.AttendanceMonth = @AttendanceMonth)
		AND (@AttendanceYear IS NULL OR ao.AttendanceYear = @AttendanceYear);

END

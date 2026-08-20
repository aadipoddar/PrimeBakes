CREATE PROCEDURE [dbo].[Insert_Attendance]
	@Id INT OUTPUT,
	@EmployeeId INT,
	@AttendanceMonth INT,
	@AttendanceYear INT,
	@DaysInMonth DECIMAL(5, 2),
	@PresentDays DECIMAL(5, 2),
	@WeeklyOffDays DECIMAL(5, 2),
	@HolidayDays DECIMAL(5, 2),
	@PaidLeaveDays DECIMAL(5, 2),
	@UnpaidLeaveDays DECIMAL(5, 2),
	@PaidDays DECIMAL(5, 2),
	@OvertimeHours DECIMAL(6, 2),
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Attendance]
		(
			[EmployeeId],
			[AttendanceMonth],
			[AttendanceYear],
			[DaysInMonth],
			[PresentDays],
			[WeeklyOffDays],
			[HolidayDays],
			[PaidLeaveDays],
			[UnpaidLeaveDays],
			[PaidDays],
			[OvertimeHours],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@EmployeeId,
			@AttendanceMonth,
			@AttendanceYear,
			@DaysInMonth,
			@PresentDays,
			@WeeklyOffDays,
			@HolidayDays,
			@PaidLeaveDays,
			@UnpaidLeaveDays,
			@PaidDays,
			@OvertimeHours,
			@Remarks,
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Attendance]
		SET [EmployeeId] = @EmployeeId,
			[AttendanceMonth] = @AttendanceMonth,
			[AttendanceYear] = @AttendanceYear,
			[DaysInMonth] = @DaysInMonth,
			[PresentDays] = @PresentDays,
			[WeeklyOffDays] = @WeeklyOffDays,
			[HolidayDays] = @HolidayDays,
			[PaidLeaveDays] = @PaidLeaveDays,
			[UnpaidLeaveDays] = @UnpaidLeaveDays,
			[PaidDays] = @PaidDays,
			[OvertimeHours] = @OvertimeHours,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS 'Id';
END

CREATE PROCEDURE [dbo].[Load_Payroll_Overview_By_Employee_Month_Year]
	@EmployeeId INT = NULL,
	@PayrollMonth INT = NULL,
	@PayrollYear INT = NULL
AS
BEGIN

	SELECT *
	FROM Payroll_Overview po
	WHERE (@EmployeeId IS NULL OR po.EmployeeId = @EmployeeId)
		AND (@PayrollMonth IS NULL OR po.PayrollMonth = @PayrollMonth)
		AND (@PayrollYear IS NULL OR po.PayrollYear = @PayrollYear);

END

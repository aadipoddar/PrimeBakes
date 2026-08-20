CREATE PROCEDURE [dbo].[Load_EmployeeSalaryComponent_Overview_By_Employee_SalaryComponent_Date]
	@EmployeeId INT = NULL,
	@SalaryComponentId INT = NULL,
	@Date DATE = NULL
AS
BEGIN

	SELECT *
	FROM EmployeeSalaryComponent_Overview esco
	WHERE (@EmployeeId IS NULL OR esco.EmployeeId = @EmployeeId)
		AND (@SalaryComponentId IS NULL OR esco.SalaryComponentId = @SalaryComponentId)
		AND (@Date IS NULL OR esco.FromDate =
		(
			SELECT MAX(esc.FromDate)
			FROM EmployeeSalaryComponent esc
			WHERE esc.EmployeeId = esco.EmployeeId
				AND esc.SalaryComponentId = esco.SalaryComponentId
				AND esc.FromDate <= @Date
		));

END

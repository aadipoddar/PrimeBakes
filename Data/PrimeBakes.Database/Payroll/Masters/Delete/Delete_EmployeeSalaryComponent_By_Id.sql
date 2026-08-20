CREATE PROCEDURE [dbo].[Delete_EmployeeSalaryComponent_By_Id]
	@Id INT
AS
BEGIN
	DELETE FROM [dbo].[EmployeeSalaryComponent] WHERE Id = @Id

	SELECT 1 AS Success;
END

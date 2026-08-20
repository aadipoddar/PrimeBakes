CREATE VIEW [dbo].[EmployeeSalaryComponent_Overview]
AS
SELECT
	[esc].[Id],
	[esc].[EmployeeId],
	[e].[Code] AS [EmployeeCode],
	[e].[Name] AS [EmployeeName],
	[e].[LocationId],
	[e].[DepartmentId],
	[e].[DesignationId],
	[esc].[SalaryComponentId],
	[sc].[Code] AS [SalaryComponentCode],
	[sc].[Name] AS [SalaryComponentName],
	[sc].[ComponentType] AS [SalaryComponentType],
	[sc].[Sequence],
	[esc].[Amount],
	[esc].[Formula],
	[sc].[Formula] AS [SalaryComponentFormula],
	[esc].[Prorate],
	[esc].[FromDate],
	[esc].[Remarks]

FROM EmployeeSalaryComponent esc

INNER JOIN Employee e ON esc.EmployeeId = e.Id
INNER JOIN SalaryComponent sc ON esc.SalaryComponentId = sc.Id

WHERE e.[Status] = 1 AND sc.[Status] = 1;

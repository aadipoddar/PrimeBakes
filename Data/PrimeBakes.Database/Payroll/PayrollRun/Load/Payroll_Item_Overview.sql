CREATE VIEW [dbo].[Payroll_Item_Overview]
AS
SELECT
	[pd].[Id],
	[pd].[MasterId],
	[pd].[SalaryComponentId],
	[sc].[Code] AS [SalaryComponentCode],
	[sc].[Name] AS [SalaryComponentName],
	[sc].[ComponentType] AS [SalaryComponentType],
	[sc].[Sequence],
	[sc].[ShowOnPayslip],

	[pd].[Amount],
	[pd].[Formula],
	[pd].[Prorate],

	[p].[TransactionNo],
	[p].[EmployeeId],
	[e].[Code] AS [EmployeeCode],
	[e].[Name] AS [EmployeeName],
	[e].[LocationId],
	[e].[DepartmentId],
	[e].[DesignationId],

	[p].[PayrollMonth],
	[p].[PayrollYear],
	[p].[TransactionDateTime],
	[p].[DaysInMonth],
	[p].[PaidDays],
	[p].[NetPay],

	[p].[Status] AS [MasterStatus]

FROM [dbo].[PayrollDetail] pd

INNER JOIN [dbo].[Payroll] p ON pd.MasterId = p.Id
INNER JOIN [dbo].[SalaryComponent] sc ON pd.SalaryComponentId = sc.Id
INNER JOIN [dbo].[Employee] e ON p.EmployeeId = e.Id

WHERE [pd].[Status] = 1;

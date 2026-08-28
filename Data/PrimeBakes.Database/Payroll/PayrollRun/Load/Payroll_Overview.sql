CREATE VIEW [dbo].[Payroll_Overview]
AS
SELECT
	[p].[Id],
	[p].[TransactionNo],
	[p].[EmployeeId],
	[e].[Code] AS [EmployeeCode],
	[e].[Name] AS [EmployeeName],
	[e].[LocationId],
	[e].[DepartmentId],
	[d].[Name] AS [DepartmentName],
	[e].[DesignationId],
	[dg].[Name] AS [DesignationName],

	[p].[PayrollMonth],
	[p].[PayrollYear],
	[p].[TransactionDateTime],
	[p].[FinancialYearId],
	CONVERT(VARCHAR(10), fy.StartDate, 103) + ' to ' + CONVERT(VARCHAR(10), fy.EndDate, 103) AS [FinancialYear],

	[p].[AttendanceId],
	[p].[DaysInMonth],
	[p].[PaidDays],

	[p].[GrossEarnings],
	[p].[TotalDeductions],
	[p].[EmployerContribution],
	[p].[NetPay],

	[p].[Remarks],

	[p].[CreatedBy],
	[u].[Name] AS [CreatedByName],
	[p].[CreatedAt],
	[p].[CreatedFormFactor],
	[p].[CreatedPlatform],
	[p].[CreatedLatitude],
	[p].[CreatedLongitude],
	[p].[LastModifiedBy],
	[lm].[Name] AS [LastModifiedByUserName],
	[p].[LastModifiedAt],
	[p].[LastModifiedFormFactor],
	[p].[LastModifiedPlatform],
	[p].[LastModifiedLatitude],
	[p].[LastModifiedLongitude],

	CASE WHEN [p].[CreatedLatitude] IS NOT NULL AND [ul].[Latitude] IS NOT NULL THEN geography::Point([p].[CreatedLatitude], [p].[CreatedLongitude], 4326).STDistance(geography::Point([ul].[Latitude], [ul].[Longitude], 4326)) END AS CreatedUserOffset,
	CASE WHEN [p].[LastModifiedLatitude] IS NOT NULL AND [lml].[Latitude] IS NOT NULL THEN geography::Point([p].[LastModifiedLatitude], [p].[LastModifiedLongitude], 4326).STDistance(geography::Point([lml].[Latitude], [lml].[Longitude], 4326)) END AS LastModifiedUserOffset,

	[p].[Status]

FROM [dbo].[Payroll] p

INNER JOIN [dbo].[Employee] e ON p.EmployeeId = e.Id
INNER JOIN [dbo].[Department] d ON e.DepartmentId = d.Id
INNER JOIN [dbo].[Designation] dg ON e.DesignationId = dg.Id
INNER JOIN [dbo].[FinancialYear] fy ON p.FinancialYearId = fy.Id
INNER JOIN [dbo].[User] u ON p.CreatedBy = u.Id
LEFT JOIN [dbo].[User] lm ON p.LastModifiedBy = lm.Id
LEFT JOIN [dbo].[Location] ul ON u.LocationId = ul.Id
LEFT JOIN [dbo].[Location] lml ON lm.LocationId = lml.Id;

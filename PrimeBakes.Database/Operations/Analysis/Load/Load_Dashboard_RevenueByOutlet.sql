CREATE PROCEDURE [dbo].[Load_Dashboard_RevenueByOutlet]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	-- Normalize date range: strip time, use exclusive upper bound
	SET @StartDate = CAST(@StartDate AS DATE);
	SET @EndDate = DATEADD(DAY, 1, CAST(@EndDate AS DATE));

	-- Same revenue definition as the monthly trend, grouped by outlet instead of month.
	WITH OutletRevenue AS (
		SELECT LocationId, TotalAmount AS Revenue
		FROM [Sale]
		WHERE Status = 1 AND TransactionDateTime >= @StartDate AND TransactionDateTime < @EndDate

		UNION ALL

		SELECT LocationId, TotalAmount
		FROM [Bill]
		WHERE Status = 1 AND Running = 0 AND TransactionDateTime >= @StartDate AND TransactionDateTime < @EndDate

		UNION ALL

		SELECT LocationId, -TotalAmount
		FROM [SaleReturn]
		WHERE Status = 1 AND TransactionDateTime >= @StartDate AND TransactionDateTime < @EndDate
	)
	-- Code, not Name: outlet names are far too long to sit under a bar chart.
	SELECT
		l.Code AS LocationCode,
		SUM(o.Revenue) AS Revenue
	FROM
		OutletRevenue o
	INNER JOIN
		[Location] l ON l.Id = o.LocationId
	GROUP BY
		l.Code
	ORDER BY
		Revenue DESC;

END

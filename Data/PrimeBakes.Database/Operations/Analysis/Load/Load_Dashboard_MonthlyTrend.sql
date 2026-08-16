CREATE PROCEDURE [dbo].[Load_Dashboard_MonthlyTrend]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	-- Normalize date range: strip time, use exclusive upper bound
	SET @StartDate = CAST(@StartDate AS DATE);
	SET @EndDate = DATEADD(DAY, 1, CAST(@EndDate AS DATE));

	-- Revenue = store sales + settled restaurant bills, net of sale returns.
	-- A running bill is still open on the table, so it has not been earned yet.
	WITH Monthly AS (
		SELECT TransactionDateTime, TotalAmount AS Revenue, 0 AS Purchase
		FROM [Sale]
		WHERE Status = 1 AND TransactionDateTime >= @StartDate AND TransactionDateTime < @EndDate

		UNION ALL

		SELECT TransactionDateTime, TotalAmount, 0
		FROM [Bill]
		WHERE Status = 1 AND Running = 0 AND TransactionDateTime >= @StartDate AND TransactionDateTime < @EndDate

		UNION ALL

		SELECT TransactionDateTime, -TotalAmount, 0
		FROM [SaleReturn]
		WHERE Status = 1 AND TransactionDateTime >= @StartDate AND TransactionDateTime < @EndDate

		UNION ALL

		SELECT TransactionDateTime, 0, TotalAmount
		FROM [Purchase]
		WHERE Status = 1 AND TransactionDateTime >= @StartDate AND TransactionDateTime < @EndDate
	)
	SELECT
		YEAR(TransactionDateTime) AS [Year],
		MONTH(TransactionDateTime) AS [Month],
		SUM(Revenue) AS Revenue,
		SUM(Purchase) AS Purchase
	FROM
		Monthly
	GROUP BY
		YEAR(TransactionDateTime), MONTH(TransactionDateTime)
	ORDER BY
		[Year], [Month];

END

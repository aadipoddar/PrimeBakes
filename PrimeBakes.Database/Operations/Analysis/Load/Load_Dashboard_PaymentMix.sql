CREATE PROCEDURE [dbo].[Load_Dashboard_PaymentMix]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	-- Normalize date range: strip time, use exclusive upper bound
	SET @StartDate = CAST(@StartDate AS DATE);
	SET @EndDate = DATEADD(DAY, 1, CAST(@EndDate AS DATE));

	-- How customers actually paid. Returns are left out so the slices stay positive.
	WITH Payments AS (
		SELECT Cash, Card, UPI, Credit
		FROM [Sale]
		WHERE Status = 1 AND TransactionDateTime >= @StartDate AND TransactionDateTime < @EndDate

		UNION ALL

		SELECT Cash, Card, UPI, Credit
		FROM [Bill]
		WHERE Status = 1 AND Running = 0 AND TransactionDateTime >= @StartDate AND TransactionDateTime < @EndDate
	)
	SELECT 'Cash' AS PaymentMode, SUM(Cash) AS Amount FROM Payments
	UNION ALL
	SELECT 'Card', SUM(Card) FROM Payments
	UNION ALL
	SELECT 'UPI', SUM(UPI) FROM Payments
	UNION ALL
	SELECT 'Credit', SUM(Credit) FROM Payments;

END

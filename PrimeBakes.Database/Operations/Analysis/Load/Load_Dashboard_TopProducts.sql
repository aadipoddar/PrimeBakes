CREATE PROCEDURE [dbo].[Load_Dashboard_TopProducts]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	-- Normalize date range: strip time, use exclusive upper bound
	SET @StartDate = CAST(@StartDate AS DATE);
	SET @EndDate = DATEADD(DAY, 1, CAST(@EndDate AS DATE));

	-- Units sold = store sale lines + settled restaurant bill lines, net of returns.
	-- Reads the detail tables directly; the *_Item_Overview views join a dozen tables
	-- to resolve names this only needs one of.
	-- Stock transfers are deliberately excluded: HQ moving stock to an outlet is not a
	-- sale, and counting it here would double up when the outlet actually sells it.
	WITH ItemMovement AS (
		SELECT sd.ProductId, sd.Quantity, sd.Total
		FROM [dbo].[SaleDetail] sd
		INNER JOIN [dbo].[Sale] s ON sd.MasterId = s.Id
		WHERE sd.Status = 1 AND s.Status = 1
			AND s.TransactionDateTime >= @StartDate AND s.TransactionDateTime < @EndDate

		UNION ALL

		SELECT bd.ProductId, bd.Quantity, bd.Total
		FROM [dbo].[BillDetail] bd
		INNER JOIN [dbo].[Bill] b ON bd.MasterId = b.Id
		WHERE bd.Status = 1 AND b.Status = 1 AND b.Running = 0
			AND b.TransactionDateTime >= @StartDate AND b.TransactionDateTime < @EndDate

		UNION ALL

		SELECT srd.ProductId, -srd.Quantity, -srd.Total
		FROM [dbo].[SaleReturnDetail] srd
		INNER JOIN [dbo].[SaleReturn] sr ON srd.MasterId = sr.Id
		WHERE srd.Status = 1 AND sr.Status = 1
			AND sr.TransactionDateTime >= @StartDate AND sr.TransactionDateTime < @EndDate
	)
	SELECT TOP (10)
		pr.[Name] AS ItemName,
		SUM(im.Quantity) AS Quantity,
		SUM(im.Total) AS Amount
	FROM
		ItemMovement im
	INNER JOIN
		[dbo].[Product] pr ON im.ProductId = pr.Id
	GROUP BY
		pr.[Name]
	HAVING
		SUM(im.Quantity) > 0
	ORDER BY
		SUM(im.Quantity) DESC;

END

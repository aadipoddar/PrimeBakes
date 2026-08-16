CREATE PROCEDURE [dbo].[Load_Dashboard_TopRawMaterials]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	-- Normalize date range: strip time, use exclusive upper bound
	SET @StartDate = CAST(@StartDate AS DATE);
	SET @EndDate = DATEADD(DAY, 1, CAST(@EndDate AS DATE));

	-- Consumption = raw material issued to the kitchen, net of what came back unused.
	-- That is the raw material equivalent of a product being sold: it is the point the
	-- stock actually leaves the store.
	-- Ranked by value, not quantity: raw materials are measured in kg, litres and pieces,
	-- so summing quantities across them would compare flour against food colouring.
	WITH Consumption AS (
		SELECT kid.RawMaterialId, kid.Quantity, kid.Total
		FROM [dbo].[KitchenIssueDetail] kid
		INNER JOIN [dbo].[KitchenIssue] ki ON kid.MasterId = ki.Id
		WHERE kid.Status = 1 AND ki.Status = 1
			AND ki.TransactionDateTime >= @StartDate AND ki.TransactionDateTime < @EndDate

		UNION ALL

		SELECT kird.RawMaterialId, -kird.Quantity, -kird.Total
		FROM [dbo].[KitchenIssueReturnDetail] kird
		INNER JOIN [dbo].[KitchenIssueReturn] kir ON kird.MasterId = kir.Id
		WHERE kird.Status = 1 AND kir.Status = 1
			AND kir.TransactionDateTime >= @StartDate AND kir.TransactionDateTime < @EndDate
	)
	SELECT TOP (10)
		rm.[Name] AS ItemName,
		rm.[UnitOfMeasurement],
		SUM(c.Quantity) AS Quantity,
		SUM(c.Total) AS Amount
	FROM
		Consumption c
	INNER JOIN
		[dbo].[RawMaterial] rm ON c.RawMaterialId = rm.Id
	GROUP BY
		rm.[Name], rm.[UnitOfMeasurement]
	HAVING
		SUM(c.Total) > 0
	ORDER BY
		SUM(c.Total) DESC;

END

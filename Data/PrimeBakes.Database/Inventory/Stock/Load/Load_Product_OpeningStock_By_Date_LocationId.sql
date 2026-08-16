CREATE PROCEDURE [dbo].[Load_Product_OpeningStock_By_Date_LocationId]
	@FromDate DATETIME,
	@LocationId INT
AS
BEGIN
	SELECT
		ProductId,
		SUM(Quantity) AS Quantity
	FROM [ProductStock] WITH (NOLOCK)
	WHERE [TransactionDateTime] < @FromDate
		AND LocationId = @LocationId
	GROUP BY ProductId;
END

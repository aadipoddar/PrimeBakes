CREATE PROCEDURE [dbo].[Load_ProductLocation_Overview_By_Product_Location_Date]
	@ProductId INT = NULL,
	@LocationId INT = NULL,
	@Date DATE = NULL
AS
BEGIN

	SET NOCOUNT ON;

	IF @Date IS NULL
	BEGIN
		SELECT *
		FROM ProductLocation_Overview
		WHERE (@ProductId IS NULL OR ProductId = @ProductId)
			AND (@LocationId IS NULL OR LocationId = @LocationId);

		RETURN;
	END;

	WITH Latest AS
	(
		SELECT ProductId, LocationId, MAX(FromDate) AS FromDate
		FROM ProductLocation
		WHERE FromDate <= @Date
			AND (@ProductId IS NULL OR ProductId = @ProductId)
			AND (@LocationId IS NULL OR LocationId = @LocationId)
		GROUP BY ProductId, LocationId
	)

	SELECT plo.*
	FROM ProductLocation_Overview plo
	INNER JOIN Latest l
		ON l.ProductId = plo.ProductId
		AND l.LocationId = plo.LocationId
		AND l.FromDate = plo.FromDate;

END

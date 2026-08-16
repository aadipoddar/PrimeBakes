CREATE PROCEDURE [dbo].[Load_ProductLocation_Overview_By_Product_Location_Date]
	@ProductId INT = NULL,
	@LocationId INT = NULL,
	@Date DATE = NULL
AS
BEGIN

	SELECT *
	FROM ProductLocation_Overview plo
	WHERE (@ProductId IS NULL OR plo.ProductId = @ProductId)
		AND (@LocationId IS NULL OR plo.LocationId = @LocationId)
		AND (@Date IS NULL OR plo.FromDate =
		(
			SELECT MAX(pl.FromDate)
			FROM ProductLocation pl
			WHERE pl.ProductId = plo.ProductId
				AND pl.LocationId = plo.LocationId
				AND pl.FromDate <= @Date
		));

END

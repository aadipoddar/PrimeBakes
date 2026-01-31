CREATE PROCEDURE [dbo].[Load_ProductLocation_Overview_By_Product_Location]
	@ProductId INT,
	@LocationId INT
AS
BEGIN

	IF @ProductId IS NOT NULL
	BEGIN
		SELECT *
		FROM ProductLocation_Overview
		WHERE ProductId = @ProductId
	END

	ELSE IF @LocationId IS NOT NULL
	BEGIN
		SELECT *
		FROM ProductLocation_Overview
		WHERE LocationId = @LocationId
	END
END
CREATE PROCEDURE [dbo].[Load_ActiveItems_By_Category]
	@CategoryId INT
AS
BEGIN

	SELECT
		*
	FROM Item
	WHERE Item.CategoryId = @CategoryId
		AND Status = 1;

END;
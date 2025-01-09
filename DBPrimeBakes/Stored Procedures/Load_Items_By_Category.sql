CREATE PROCEDURE [dbo].[Load_Items_By_Category]
	@CategoryId INT
AS
BEGIN

	SELECT
		*
	FROM Item
	WHERE Item.CategoryId = @CategoryId

END;
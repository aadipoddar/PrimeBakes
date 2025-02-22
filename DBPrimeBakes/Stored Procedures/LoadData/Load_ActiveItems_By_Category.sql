CREATE PROCEDURE [dbo].[Load_ActiveItems_By_Category]
	@CategoryId INT,
	@UserCategoryId INT
AS
BEGIN

	SELECT
		*
	FROM Item
	WHERE Item.[ItemCategoryId] = @CategoryId
		AND Item.[UserCategoryId] = @UserCategoryId
		AND Status = 1;

END;
CREATE PROCEDURE Update_Item
	@Id INT,
	@ItemCategoryId INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
    @UserCategoryId INT,
    @Status BIT
AS
BEGIN

    UPDATE Item
    SET
		ItemCategoryId = @ItemCategoryId,
        Code = @Code,
        Name = @Name,
        UserCategoryId = @UserCategoryId,
		Status = @Status
    WHERE Id = @Id;

END;
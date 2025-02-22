CREATE PROCEDURE Insert_Item
	@Id INT,
	@ItemCategoryId INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
	@UserCategoryId INT,
    @Status BIT
AS
BEGIN

	INSERT INTO Item
	VALUES (
		@ItemCategoryId,
		@Code,
        @Name,
		@UserCategoryId,
		@Status
    );

END
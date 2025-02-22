CREATE PROCEDURE Insert_User
	@Id INT,
	@Name VARCHAR(100),
	@Code VARCHAR(100),
    @Password VARCHAR(100),
	@CustomerId INT,
	@UserCategoryId INT,
    @Status BIT
AS
BEGIN
	INSERT INTO [User]
	VALUES (
        @Name,
		@Code,
        @Password,
		@CustomerId,
		@UserCategoryId,
		@Status
    );

END
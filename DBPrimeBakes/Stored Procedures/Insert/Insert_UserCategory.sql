CREATE PROCEDURE Insert_UserCategory
	@Id INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
    @Status BIT
AS
BEGIN

	INSERT INTO UserCategory
	VALUES (
		@Code,
        @Name,
		@Status
    );

END
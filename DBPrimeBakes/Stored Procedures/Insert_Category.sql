CREATE PROCEDURE Insert_Category
	@Id INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
    @Status BIT
AS
BEGIN

	INSERT INTO Category
	VALUES (
		@Code,
        @Name,
		@Status
    );

END
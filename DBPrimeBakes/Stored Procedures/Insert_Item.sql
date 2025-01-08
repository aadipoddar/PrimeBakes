CREATE PROCEDURE Insert_Item
	@Id INT,
	@CategoryId INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
    @Status BIT
AS
BEGIN

	INSERT INTO Item
	VALUES (
		@CategoryId,
		@Code,
        @Name,
		@Status
    );

END
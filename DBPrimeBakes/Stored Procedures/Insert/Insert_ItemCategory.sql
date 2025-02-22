CREATE PROCEDURE Insert_ItemCategory
	@Id INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
    @Status BIT
AS
BEGIN

	INSERT INTO [ItemCategory]
	VALUES (
		@Code,
        @Name,
		@Status
    );

END
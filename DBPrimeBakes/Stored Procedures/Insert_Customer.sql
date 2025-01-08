CREATE PROCEDURE Insert_Customer
	@Id INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
    @Status BIT
AS
BEGIN
	INSERT INTO Customer
	VALUES (
		@Code,
        @Name,
		@Status
    );

END
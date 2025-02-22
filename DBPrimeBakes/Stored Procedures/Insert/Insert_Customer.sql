CREATE PROCEDURE Insert_Customer
	@Id INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
	@Email VARCHAR(50),
    @Status BIT
AS
BEGIN
	INSERT INTO Customer
	VALUES (
		@Code,
        @Name,
        @Email,
		@Status
    );

END
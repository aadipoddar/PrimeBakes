CREATE PROCEDURE Update_Customer
	@Id INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
	@Email VARCHAR(50),
    @Status BIT
AS
BEGIN
    UPDATE Customer
    SET
        Code = @Code,
        Name = @Name,
        Email = @Email,
		Status = @Status
    WHERE Id = @Id;
END;
CREATE PROCEDURE Update_Customer
	@Id INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
    @Status BIT
AS
BEGIN
    UPDATE Customer
    SET
        Code = @Code,
        Name = @Name,
		Status = @Status
    WHERE Id = @Id;
END;
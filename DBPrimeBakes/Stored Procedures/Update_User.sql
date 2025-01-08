CREATE PROCEDURE Update_User
	@Id INT,
	@Name VARCHAR(100),
    @Password VARCHAR(100),
    @Status BIT
AS
BEGIN
    UPDATE [User]
    SET
        Name = @Name,
        Password = @Password,
		Status = @Status
    WHERE Id = @Id;
END;
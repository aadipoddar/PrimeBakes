CREATE PROCEDURE Update_UserCategory
	@Id INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
    @Status BIT
AS
BEGIN

    UPDATE [UserCategory]
    SET
        Code = @Code,
        Name = @Name,
		Status = @Status
    WHERE Id = @Id;

END;
CREATE PROCEDURE Update_Category
	@Id INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
    @Status BIT
AS
BEGIN

    UPDATE Category
    SET
        Code = @Code,
        Name = @Name,
		Status = @Status
    WHERE Id = @Id;

END;
CREATE PROCEDURE Update_Item
	@Id INT,
	@CategoryId INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
    @Status BIT
AS
BEGIN

    UPDATE Item
    SET
		CategoryId = @CategoryId,
        Code = @Code,
        Name = @Name,
		Status = @Status
    WHERE Id = @Id;

END;
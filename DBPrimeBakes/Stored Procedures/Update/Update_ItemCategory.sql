CREATE PROCEDURE Update_ItemCategory
	@Id INT,
    @Code VARCHAR(100),
	@Name VARCHAR(100),
    @Status BIT
AS
BEGIN

    UPDATE [ItemCategory]
    SET
        Code = @Code,
        Name = @Name,
		Status = @Status
    WHERE Id = @Id;

END;
﻿CREATE PROCEDURE Update_User
	@Id INT,
	@Name VARCHAR(100),
	@Code VARCHAR(100),
    @Password VARCHAR(100),
    @CustomerId INT,
    @Status BIT
AS
BEGIN
    UPDATE [User]
    SET
        Name = @Name,
        Code = @Code,
        Password = @Password,
        CustomerId = @CustomerId,
		Status = @Status
    WHERE Id = @Id;
END;
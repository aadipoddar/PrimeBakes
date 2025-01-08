CREATE PROCEDURE Insert_Order
	@Id INT OUTPUT,
	@UserId INT,
	@CustomerId INT,
	@DateTime DATETIME,
	@Status BIT
AS
BEGIN
	INSERT INTO [Order]
	(UserId, CustomerId)
	OUTPUT INSERTED.Id
	VALUES (@UserId, @CustomerId);

END
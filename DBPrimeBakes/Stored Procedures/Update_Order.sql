CREATE PROCEDURE Update_Order
	@Id INT,
	@UserId INT,
	@CustomerId INT,
	@DateTime DATETIME,
	@Status BIT
AS
BEGIN
	SET NOCOUNT ON;

	UPDATE [Order]
	SET UserId = @UserId,
		CustomerId = @CustomerId,
		Status = @Status
	WHERE Id = @Id;

END
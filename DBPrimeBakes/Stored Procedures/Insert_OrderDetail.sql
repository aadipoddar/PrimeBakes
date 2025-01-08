CREATE PROCEDURE Insert_OrderDetail
	@Id INT,
	@OrderId INT,
	@ItemId INT,
	@Quantity INT
AS
BEGIN
	INSERT INTO OrderDetail
	VALUES (
		@OrderId,
		@ItemId,
		@Quantity
    );

END
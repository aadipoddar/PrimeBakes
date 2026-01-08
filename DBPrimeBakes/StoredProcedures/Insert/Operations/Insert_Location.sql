CREATE PROCEDURE [dbo].[Insert_Location]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Code VARCHAR(10),
	@Discount DECIMAL(5, 2),
	@LedgerId INT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Location] (Name, [Code], Discount, LedgerId, Remarks, Status)
		VALUES (@Name, @Code, @Discount, @LedgerId, @Remarks, @Status);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Location]
		SET
			Name = @Name,
			[Code] = @Code,
			Discount = @Discount,
			LedgerId = @LedgerId,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;
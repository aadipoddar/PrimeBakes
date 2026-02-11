CREATE PROCEDURE [dbo].[Insert_User]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Passcode SMALLINT,
	@LocationId INT,
	@Sales BIT,
	@Order BIT,
	@Inventory BIT,
	@Accounts BIT,
	@Reports BIT,
	@Admin BIT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[User] (Name, Passcode, LocationId, [Order], Inventory, Accounts, Admin, Sales, Reports, Remarks, Status)
		VALUES (@Name, @Passcode, @LocationId, @Order, @Inventory, @Accounts, @Admin, @Sales, @Reports, @Remarks, @Status);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[User]
		SET Name = @Name,
			Passcode = @Passcode,
			LocationId = @LocationId,
			Sales = @Sales,
			[Order] = @Order,
			Inventory = @Inventory,
			Accounts = @Accounts,
			Reports = @Reports,
			Admin = @Admin,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;
CREATE PROCEDURE [dbo].[Insert_User]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Passcode SMALLINT,
	@LocationId INT,
	@Accounts BIT,
	@Inventory BIT,
	@Store BIT,
	@Restaurant BIT,
	@Reports BIT,
	@Admin BIT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[User]
		(
			Name,
			Passcode,
			LocationId,
			Accounts,
			Inventory,
			Store,
			Restaurant,
			Reports,
			Admin,
			Remarks,
			Status
		)
		VALUES
		(
			@Name,
			@Passcode,
			@LocationId,
			@Accounts, 
			@Inventory, 
			@Store, 
			@Restaurant, 
			@Reports,
			@Admin, 
			@Remarks, 
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[User]
		SET Name = @Name,
			Passcode = @Passcode,
			LocationId = @LocationId,
			Accounts = @Accounts,
			Inventory = @Inventory,
			Store = @Store,
			Restaurant = @Restaurant,
			Reports = @Reports,
			Admin = @Admin,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;
CREATE PROCEDURE [dbo].[Insert_User]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Passcode SMALLINT,
	@LocationId INT,
	@ChangeProductFinancial BIT,
	@Accounts BIT,
	@Inventory BIT,
	@Store BIT,
	@Restaurant BIT,
	@Payroll BIT,
	@Reports BIT,
	@Admin BIT,
	@Remarks VARCHAR(MAX),
	@LastLoginTime DATETIME,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[User]
		(
			[Name],
			[Passcode],
			[LocationId],
			[ChangeProductFinancial],
			[Accounts],
			[Inventory],
			[Store],
			[Restaurant],
			[Payroll],
			[Reports],
			[Admin],
			[Remarks],
			[LastLoginTime],
			[Status]
		)
		VALUES
		(
			@Name,
			@Passcode,
			@LocationId,
			@ChangeProductFinancial,
			@Accounts, 
			@Inventory, 
			@Store, 
			@Restaurant, 
			@Payroll, 
			@Reports,
			@Admin, 
			@Remarks, 
			@LastLoginTime, 
			@Status
		);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[User]
		SET [Name] = @Name,
			[Passcode] = @Passcode,
			[LocationId] = @LocationId,
			[ChangeProductFinancial] = @ChangeProductFinancial,
			[Accounts] = @Accounts,
			[Inventory] = @Inventory,
			[Store] = @Store,
			[Restaurant] = @Restaurant,
			[Payroll] = @Payroll,
			[Reports] = @Reports,
			[Admin] = @Admin,
			[Remarks] = @Remarks,
			[LastLoginTime] = @LastLoginTime,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END;
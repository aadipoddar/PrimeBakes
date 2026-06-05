CREATE PROCEDURE [dbo].[Insert_Ledger]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@GroupId INT,
	@AccountTypeId INT,
	@Code VARCHAR(10),
	@StateUTId INT,
	@GSTNo VARCHAR(MAX),
	@PANNo VARCHAR(MAX),
	@CINNo VARCHAR(MAX),
	@Alias VARCHAR(MAX),
	@Phone VARCHAR(10),
	@Email VARCHAR(MAX),
	@Address VARCHAR(MAX),
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Ledger]
		(
			[Name],
			[GroupId],
			[AccountTypeId],
			[Code],
			[StateUTId],
			[GSTNo],
			[PANNo],
			[CINNo],
			[Alias],
			[Phone],
			[Email],
			[Address],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@GroupId,
			@AccountTypeId,
			@Code,
			@StateUTId,
			@GSTNo,
			@PANNo,
			@CINNo,
			@Alias,
			@Phone,
			@Email,
			@Address,
			@Remarks,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Ledger]
		SET
			[Name] = @Name,
			[GroupId] = @GroupId,
			[AccountTypeId] = @AccountTypeId,
			[Code] = @Code,
			[StateUTId] = @StateUTId,
			[GSTNo] = @GSTNo,
			[PANNo] = @PANNo,
			[CINNo] = @CINNo,
			[Alias] = @Alias,
			[Phone] = @Phone,
			[Email] = @Email,
			[Address] = @Address,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END
CREATE PROCEDURE [dbo].[Insert_Company]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
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
		INSERT INTO [dbo].[Company]
		(
			[Name],
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
		UPDATE [dbo].[Company]
		SET
			[Name] = @Name,
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
CREATE PROCEDURE [dbo].[Insert_Voucher]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Code VARCHAR(10),
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Voucher]
		(
			[Name],
			[Code],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Code,
			@Remarks,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Voucher]
		SET
			[Name] = @Name,
			[Code] = @Code,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END
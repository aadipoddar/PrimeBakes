CREATE PROCEDURE [dbo].[Insert_Group]
	@Id INT OUTPUT,
	@Name VARCHAR(250),
	@Nature VARCHAR(50),
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Group]
		(
			[Name],
			[Nature],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@Nature,
			@Remarks,
			@Status
		);
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Group]
		SET
			[Name] = @Name,
			[Nature] = @Nature,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE
			[Id] = @Id;
	END

	SELECT @Id AS Id;
END
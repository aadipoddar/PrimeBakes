CREATE PROCEDURE [dbo].[Insert_Group]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@NatureId INT,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Group]
		(
			[Name],
			[NatureId],
			[Remarks],
			[Status]
		)
		VALUES
		(
			@Name,
			@NatureId,
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
			[NatureId] = @NatureId,
			[Remarks] = @Remarks,
			[Status] = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END
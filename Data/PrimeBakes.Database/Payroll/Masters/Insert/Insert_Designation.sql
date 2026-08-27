CREATE PROCEDURE [dbo].[Insert_Designation]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@Code VARCHAR(10),
	@Remarks VARCHAR(MAX) = NULL,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[Designation] (Name, Code, Remarks, Status)
		VALUES (@Name, @Code, @Remarks, @Status);
		
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[Designation]
		SET [Name] = @Name, Code = @Code, Remarks = @Remarks, Status = @Status
		WHERE [Id] = @Id;
	END

	SELECT @Id AS Id;
END;

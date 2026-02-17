CREATE PROCEDURE [dbo].[Insert_DiningArea]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@LocationId INT,
	@Remarks VARCHAR(MAX) = NULL,
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[DiningArea] (Name, LocationId, Remarks, Status)
		VALUES (@Name, @LocationId, @Remarks, @Status);
		
		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[DiningArea]
		SET Name = @Name, LocationId = @LocationId, Remarks = @Remarks, Status = @Status
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;

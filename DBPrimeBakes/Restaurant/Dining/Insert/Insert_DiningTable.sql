CREATE PROCEDURE [dbo].[Insert_DiningTable]
	@Id INT OUTPUT,
	@Name VARCHAR(500),
	@DiningAreaId INT,
	@Remarks VARCHAR(MAX) = NULL,
	@Status BIT,
	@LayoutJson VARCHAR(MAX) = NULL
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[DiningTable] (Name, DiningAreaId, Remarks, Status, LayoutJson)
		VALUES (@Name, @DiningAreaId, @Remarks, @Status, @LayoutJson);

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[DiningTable]
		SET Name = @Name, DiningAreaId = @DiningAreaId, Remarks = @Remarks, Status = @Status, LayoutJson = @LayoutJson
		WHERE Id = @Id;
	END

	SELECT @Id AS Id;
END;

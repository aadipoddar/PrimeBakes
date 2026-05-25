CREATE PROCEDURE [dbo].[Delete_ProductLocation_By_Id]
	@Id INT
AS
BEGIN
	DELETE FROM [dbo].[ProductLocation]
	WHERE Id = @Id
END
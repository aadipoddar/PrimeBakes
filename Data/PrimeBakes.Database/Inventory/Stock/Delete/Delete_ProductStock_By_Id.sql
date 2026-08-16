CREATE PROCEDURE [dbo].[Delete_ProductStock_By_Id]
	@Id INT
AS
BEGIN
	DELETE FROM [dbo].[ProductStock] WHERE [Id] = @Id;

	SELECT 1 AS Success;
END
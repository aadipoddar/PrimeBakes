CREATE PROCEDURE [dbo].[Delete_PulpTrace_By_Id]
	@Id INT
AS
BEGIN
	DELETE FROM [dbo].[PulpTrace] WHERE [Id] = @Id;

	SELECT 1 AS Success;
END
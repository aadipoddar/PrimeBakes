CREATE PROCEDURE [dbo].[Delete_OfflineQueue_By_Id]
	@Id INT
AS
BEGIN
	DELETE FROM [dbo].[OfflineQueue] WHERE [Id] = @Id;

	SELECT 1 AS Success;
END
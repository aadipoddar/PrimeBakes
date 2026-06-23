CREATE PROCEDURE [dbo].[Delete_BillDetail_By_Id]
	@Id INT
AS
BEGIN
	DELETE FROM [dbo].[BillDetail] WHERE [Id] = @Id;

	SELECT 1 AS Success;
END
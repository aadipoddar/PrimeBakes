CREATE PROCEDURE [dbo].[Delete_BillDetail_By_Id]
	@Id INT
AS
BEGIN
	DELETE FROM [dbo].[BillDetail]
	WHERE [Id] = @Id;
END
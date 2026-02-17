CREATE PROCEDURE [dbo].[Load_RunningBill_By_LocationId]
	@LocationId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[Bill]
	WHERE [Running] = 1
	AND [LocationId] = @LocationId
END
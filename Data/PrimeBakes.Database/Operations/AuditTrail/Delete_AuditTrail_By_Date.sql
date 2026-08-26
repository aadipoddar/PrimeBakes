CREATE PROCEDURE [dbo].[Delete_AuditTrail_By_Date]
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	SET @StartDate = CAST(@StartDate AS DATE);
	SET @EndDate = DATEADD(DAY, 1, CAST(@EndDate AS DATE));

	DELETE FROM [dbo].[AuditTrail]
	WHERE [TransactionDateTime] >= @StartDate
		AND [TransactionDateTime] < @EndDate;

	SELECT @@ROWCOUNT AS Deleted;
END
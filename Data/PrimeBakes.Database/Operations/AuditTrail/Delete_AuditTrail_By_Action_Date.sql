CREATE PROCEDURE [dbo].[Delete_AuditTrail_By_Action_Date]
	@Action VARCHAR(20) = NULL,
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	SET @StartDate = CAST(@StartDate AS DATE);
	SET @EndDate = DATEADD(DAY, 1, CAST(@EndDate AS DATE));

	DELETE FROM [dbo].[AuditTrail]
	WHERE [TransactionDateTime] >= @StartDate
		AND [TransactionDateTime] < @EndDate
		AND (@Action IS NULL OR [Action] = @Action);

	SELECT @@ROWCOUNT AS Deleted;
END
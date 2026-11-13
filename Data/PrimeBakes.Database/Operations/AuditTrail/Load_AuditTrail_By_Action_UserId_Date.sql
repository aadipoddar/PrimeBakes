CREATE PROCEDURE [dbo].[Load_AuditTrail_By_Action_UserId_Date]
	@Action VARCHAR(20),
	@UserId INT,
	@StartDate DATETIME,
	@EndDate DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	SET @StartDate = CAST(@StartDate AS DATE);
	SET @EndDate = DATEADD(DAY, 1, CAST(@EndDate AS DATE));

	SELECT *
	FROM [dbo].[AuditTrail]
	WHERE [Action] = @Action
		AND [CreatedBy] = @UserId
		AND [TransactionDateTime] >= @StartDate
		AND [TransactionDateTime] < @EndDate

END
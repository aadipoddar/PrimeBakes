CREATE PROCEDURE [dbo].[Load_Sale_By_FinancialAccountingId]
	@FinancialAccountingId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[Sale]
	WHERE [FinancialAccountingId] IS NOT NULL
		AND [FinancialAccountingId] = @FinancialAccountingId
		AND [Status] = 1
END

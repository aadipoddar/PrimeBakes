CREATE PROCEDURE [dbo].[Load_Bill_By_FinancialAccountingId]
	@FinancialAccountingId INT
AS
BEGIN
	SELECT *
	FROM [dbo].[Bill]
	WHERE [FinancialAccountingId] IS NOT NULL
		AND [FinancialAccountingId] = @FinancialAccountingId
		AND [Status] = 1
END
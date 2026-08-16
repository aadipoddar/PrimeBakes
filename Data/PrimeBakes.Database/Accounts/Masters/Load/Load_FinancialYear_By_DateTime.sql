CREATE PROCEDURE [dbo].[Load_FinancialYear_By_DateTime]
	@TransactionDateTime DateTime
AS
BEGIN
	SELECT *
	FROM FinancialYear
	WHERE CAST(@TransactionDateTime AS DATE) BETWEEN CAST(StartDate AS DATE) AND CAST(EndDate AS DATE)
	AND Status = 1
END
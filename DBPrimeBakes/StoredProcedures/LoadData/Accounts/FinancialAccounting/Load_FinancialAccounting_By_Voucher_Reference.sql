CREATE PROCEDURE [dbo].[Load_FinancialAccounting_By_Voucher_Reference]
	@VoucherId INT,
	@ReferenceId INT,
	@ReferenceNo VARCHAR(MAX)
AS
BEGIN
	SELECT *
	FROM [FinancialAccounting]
	WHERE VoucherId = @VoucherId
	AND ReferenceId = @ReferenceId
	AND ReferenceNo = @ReferenceNo
	AND Status = 1;
END
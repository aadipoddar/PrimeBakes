CREATE PROCEDURE [dbo].[Insert_PayrollDetail_List]
	@PayrollDetails [dbo].[PayrollDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[PayrollDetail]
	SET
		[MasterId] = [PayrollDetails].[MasterId],
		[SalaryComponentId] = [PayrollDetails].[SalaryComponentId],
		[Amount] = [PayrollDetails].[Amount],
		[Formula] = [PayrollDetails].[Formula],
		[Prorate] = [PayrollDetails].[Prorate],
		[Status] = [PayrollDetails].[Status]
	FROM @PayrollDetails AS [PayrollDetails]
	WHERE [dbo].[PayrollDetail].[Id] = [PayrollDetails].[Id];

	INSERT INTO [dbo].[PayrollDetail]
	(
		[MasterId],
		[SalaryComponentId],
		[Amount],
		[Formula],
		[Prorate],
		[Status]
	)
	SELECT
		[MasterId],
		[SalaryComponentId],
		[Amount],
		[Formula],
		[Prorate],
		[Status]
	FROM @PayrollDetails
	WHERE [Id] = 0;
END;
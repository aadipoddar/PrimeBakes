CREATE PROCEDURE [dbo].[Insert_FinancialAccountingLedger_List]
	@FinancialAccountingLedgers [dbo].[FinancialAccountingLedgerType] READONLY
AS
BEGIN
	UPDATE [dbo].[FinancialAccountingLedger]
	SET
		[MasterId] = [FinancialAccountingLedgers].[MasterId],
		[LedgerId] = [FinancialAccountingLedgers].[LedgerId],
		[ReferenceId] = [FinancialAccountingLedgers].[ReferenceId],
		[ReferenceType] = [FinancialAccountingLedgers].[ReferenceType],
		[ReferenceNo] = [FinancialAccountingLedgers].[ReferenceNo],
		[Debit] = [FinancialAccountingLedgers].[Debit],
		[Credit] = [FinancialAccountingLedgers].[Credit],
		[InstrumentNo] = [FinancialAccountingLedgers].[InstrumentNo],
		[InstrumentDate] = [FinancialAccountingLedgers].[InstrumentDate],
		[ClearingDate] = [FinancialAccountingLedgers].[ClearingDate],
		[Remarks] = [FinancialAccountingLedgers].[Remarks],
		[Status] = [FinancialAccountingLedgers].[Status]
	FROM @FinancialAccountingLedgers AS [FinancialAccountingLedgers]
	WHERE [dbo].[FinancialAccountingLedger].[Id] = [FinancialAccountingLedgers].[Id];

	INSERT INTO [dbo].[FinancialAccountingLedger]
	(
		[MasterId],
		[LedgerId],
		[ReferenceId],
		[ReferenceType],
		[ReferenceNo],
		[Debit],
		[Credit],
		[InstrumentNo],
		[InstrumentDate],
		[ClearingDate],
		[Remarks],
		[Status]
	)
	SELECT
		[MasterId],
		[LedgerId],
		[ReferenceId],
		[ReferenceType],
		[ReferenceNo],
		[Debit],
		[Credit],
		[InstrumentNo],
		[InstrumentDate],
		[ClearingDate],
		[Remarks],
		[Status]
	FROM @FinancialAccountingLedgers
	WHERE [Id] = 0;
END;
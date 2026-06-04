CREATE PROCEDURE [dbo].[Insert_FinancialAccountingDetail]
	@Id INT OUTPUT,
	@MasterId INT,
	@LedgerId INT,
	@ReferenceType VARCHAR(MAX),
	@ReferenceId INT,
	@ReferenceNo VARCHAR(MAX),
	@Debit MONEY,
	@Credit MONEY,
	@Remarks VARCHAR(MAX),
	@Status BIT
AS
BEGIN
	IF @Id = 0
	BEGIN
		INSERT INTO [dbo].[FinancialAccountingDetail]
		(
			[MasterId],
			LedgerId,
			ReferenceType,
			ReferenceId,
			ReferenceNo,
			Debit,
			Credit,
			Remarks,
			Status
		) VALUES
		(
			@MasterId,
			@LedgerId,
			@ReferenceType,
			@ReferenceId,
			@ReferenceNo,
			@Debit,
			@Credit,
			@Remarks,
			@Status
		)

		SET @Id = SCOPE_IDENTITY();
	END

	ELSE
	BEGIN
		UPDATE [dbo].[FinancialAccountingDetail]
		SET
			[MasterId] = @MasterId,
			LedgerId = @LedgerId,
			ReferenceType = @ReferenceType,
			ReferenceId = @ReferenceId,
			ReferenceNo = @ReferenceNo,
			Debit = @Debit,
			Credit = @Credit,
			Remarks = @Remarks,
			Status = @Status
		WHERE Id = @Id
	END

	SELECT @Id AS Id
END
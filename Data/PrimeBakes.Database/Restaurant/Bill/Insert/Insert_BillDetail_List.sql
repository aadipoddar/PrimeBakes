CREATE PROCEDURE [dbo].[Insert_BillDetail_List]
	@BillDetails [dbo].[BillDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[BillDetail]
	SET
		[MasterId] = [BillDetails].[MasterId],
		[ProductId] = [BillDetails].[ProductId],
		[Quantity] = [BillDetails].[Quantity],
		[Rate] = [BillDetails].[Rate],
		[BaseTotal] = [BillDetails].[BaseTotal],
		[DiscountPercent] = [BillDetails].[DiscountPercent],
		[DiscountAmount] = [BillDetails].[DiscountAmount],
		[AfterDiscount] = [BillDetails].[AfterDiscount],
		[CGSTPercent] = [BillDetails].[CGSTPercent],
		[CGSTAmount] = [BillDetails].[CGSTAmount],
		[SGSTPercent] = [BillDetails].[SGSTPercent],
		[SGSTAmount] = [BillDetails].[SGSTAmount],
		[IGSTPercent] = [BillDetails].[IGSTPercent],
		[IGSTAmount] = [BillDetails].[IGSTAmount],
		[TotalTaxAmount] = [BillDetails].[TotalTaxAmount],
		[InclusiveTax] = [BillDetails].[InclusiveTax],
		[Total] = [BillDetails].[Total],
		[NetRate] = [BillDetails].[NetRate],
		[Remarks] = [BillDetails].[Remarks],
		[KOTPrint] = [BillDetails].[KOTPrint],
		[Status] = [BillDetails].[Status]
	FROM @BillDetails AS [BillDetails]
	WHERE [dbo].[BillDetail].[Id] = [BillDetails].[Id];

	INSERT INTO [dbo].[BillDetail]
	(
		[MasterId],
		[ProductId],
		[Quantity],
		[Rate],
		[BaseTotal],
		[DiscountPercent],
		[DiscountAmount],
		[AfterDiscount],
		[CGSTPercent],
		[CGSTAmount],
		[SGSTPercent],
		[SGSTAmount],
		[IGSTPercent],
		[IGSTAmount],
		[TotalTaxAmount],
		[InclusiveTax],
		[Total],
		[NetRate],
		[Remarks],
		[KOTPrint],
		[Status]
	)
	SELECT
		[MasterId],
		[ProductId],
		[Quantity],
		[Rate],
		[BaseTotal],
		[DiscountPercent],
		[DiscountAmount],
		[AfterDiscount],
		[CGSTPercent],
		[CGSTAmount],
		[SGSTPercent],
		[SGSTAmount],
		[IGSTPercent],
		[IGSTAmount],
		[TotalTaxAmount],
		[InclusiveTax],
		[Total],
		[NetRate],
		[Remarks],
		[KOTPrint],
		[Status]
	FROM @BillDetails
	WHERE [Id] = 0;
END;
CREATE PROCEDURE [dbo].[Insert_SaleReturnDetail_List]
	@SaleReturnDetails [dbo].[SaleReturnDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[SaleReturnDetail]
	SET
		[MasterId] = [SaleReturnDetails].[MasterId],
		[ProductId] = [SaleReturnDetails].[ProductId],
		[Quantity] = [SaleReturnDetails].[Quantity],
		[Rate] = [SaleReturnDetails].[Rate],
		[BaseTotal] = [SaleReturnDetails].[BaseTotal],
		[DiscountPercent] = [SaleReturnDetails].[DiscountPercent],
		[DiscountAmount] = [SaleReturnDetails].[DiscountAmount],
		[AfterDiscount] = [SaleReturnDetails].[AfterDiscount],
		[CGSTPercent] = [SaleReturnDetails].[CGSTPercent],
		[CGSTAmount] = [SaleReturnDetails].[CGSTAmount],
		[SGSTPercent] = [SaleReturnDetails].[SGSTPercent],
		[SGSTAmount] = [SaleReturnDetails].[SGSTAmount],
		[IGSTPercent] = [SaleReturnDetails].[IGSTPercent],
		[IGSTAmount] = [SaleReturnDetails].[IGSTAmount],
		[TotalTaxAmount] = [SaleReturnDetails].[TotalTaxAmount],
		[InclusiveTax] = [SaleReturnDetails].[InclusiveTax],
		[Total] = [SaleReturnDetails].[Total],
		[NetRate] = [SaleReturnDetails].[NetRate],
		[Remarks] = [SaleReturnDetails].[Remarks],
		[Status] = [SaleReturnDetails].[Status]
	FROM @SaleReturnDetails AS [SaleReturnDetails]
	WHERE [dbo].[SaleReturnDetail].[Id] = [SaleReturnDetails].[Id];

	INSERT INTO [dbo].[SaleReturnDetail]
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
		[Status]
	FROM @SaleReturnDetails
	WHERE [Id] = 0;
END;
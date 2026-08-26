CREATE PROCEDURE [dbo].[Insert_SaleDetail_List]
	@SaleDetails [dbo].[SaleDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[SaleDetail]
	SET
		[MasterId] = [SaleDetails].[MasterId],
		[ProductId] = [SaleDetails].[ProductId],
		[Quantity] = [SaleDetails].[Quantity],
		[Rate] = [SaleDetails].[Rate],
		[BaseTotal] = [SaleDetails].[BaseTotal],
		[DiscountPercent] = [SaleDetails].[DiscountPercent],
		[DiscountAmount] = [SaleDetails].[DiscountAmount],
		[AfterDiscount] = [SaleDetails].[AfterDiscount],
		[CGSTPercent] = [SaleDetails].[CGSTPercent],
		[CGSTAmount] = [SaleDetails].[CGSTAmount],
		[SGSTPercent] = [SaleDetails].[SGSTPercent],
		[SGSTAmount] = [SaleDetails].[SGSTAmount],
		[IGSTPercent] = [SaleDetails].[IGSTPercent],
		[IGSTAmount] = [SaleDetails].[IGSTAmount],
		[TotalTaxAmount] = [SaleDetails].[TotalTaxAmount],
		[InclusiveTax] = [SaleDetails].[InclusiveTax],
		[Total] = [SaleDetails].[Total],
		[NetRate] = [SaleDetails].[NetRate],
		[Remarks] = [SaleDetails].[Remarks],
		[Status] = [SaleDetails].[Status]
	FROM @SaleDetails AS [SaleDetails]
	WHERE [dbo].[SaleDetail].[Id] = [SaleDetails].[Id];

	INSERT INTO [dbo].[SaleDetail]
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
	FROM @SaleDetails
	WHERE [Id] = 0;
END;
CREATE PROCEDURE [dbo].[Insert_StockTransferDetail_List]
	@StockTransferDetails [dbo].[StockTransferDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[StockTransferDetail]
	SET
		[MasterId] = [StockTransferDetails].[MasterId],
		[ProductId] = [StockTransferDetails].[ProductId],
		[Quantity] = [StockTransferDetails].[Quantity],
		[Rate] = [StockTransferDetails].[Rate],
		[BaseTotal] = [StockTransferDetails].[BaseTotal],
		[DiscountPercent] = [StockTransferDetails].[DiscountPercent],
		[DiscountAmount] = [StockTransferDetails].[DiscountAmount],
		[AfterDiscount] = [StockTransferDetails].[AfterDiscount],
		[CGSTPercent] = [StockTransferDetails].[CGSTPercent],
		[CGSTAmount] = [StockTransferDetails].[CGSTAmount],
		[SGSTPercent] = [StockTransferDetails].[SGSTPercent],
		[SGSTAmount] = [StockTransferDetails].[SGSTAmount],
		[IGSTPercent] = [StockTransferDetails].[IGSTPercent],
		[IGSTAmount] = [StockTransferDetails].[IGSTAmount],
		[TotalTaxAmount] = [StockTransferDetails].[TotalTaxAmount],
		[InclusiveTax] = [StockTransferDetails].[InclusiveTax],
		[Total] = [StockTransferDetails].[Total],
		[NetRate] = [StockTransferDetails].[NetRate],
		[Remarks] = [StockTransferDetails].[Remarks],
		[Status] = [StockTransferDetails].[Status]
	FROM @StockTransferDetails AS [StockTransferDetails]
	WHERE [dbo].[StockTransferDetail].[Id] = [StockTransferDetails].[Id];

	INSERT INTO [dbo].[StockTransferDetail]
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
	FROM @StockTransferDetails
	WHERE [Id] = 0;
END;
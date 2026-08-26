CREATE PROCEDURE [dbo].[Insert_PurchaseReturnDetail_List]
	@PurchaseReturnDetails [dbo].[PurchaseReturnDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[PurchaseReturnDetail]
	SET
		[MasterId] = [PurchaseReturnDetails].[MasterId],
		[RawMaterialId] = [PurchaseReturnDetails].[RawMaterialId],
		[Quantity] = [PurchaseReturnDetails].[Quantity],
		[UnitOfMeasurement] = [PurchaseReturnDetails].[UnitOfMeasurement],
		[Rate] = [PurchaseReturnDetails].[Rate],
		[BaseTotal] = [PurchaseReturnDetails].[BaseTotal],
		[DiscountPercent] = [PurchaseReturnDetails].[DiscountPercent],
		[DiscountAmount] = [PurchaseReturnDetails].[DiscountAmount],
		[AfterDiscount] = [PurchaseReturnDetails].[AfterDiscount],
		[CGSTPercent] = [PurchaseReturnDetails].[CGSTPercent],
		[CGSTAmount] = [PurchaseReturnDetails].[CGSTAmount],
		[SGSTPercent] = [PurchaseReturnDetails].[SGSTPercent],
		[SGSTAmount] = [PurchaseReturnDetails].[SGSTAmount],
		[IGSTPercent] = [PurchaseReturnDetails].[IGSTPercent],
		[IGSTAmount] = [PurchaseReturnDetails].[IGSTAmount],
		[TotalTaxAmount] = [PurchaseReturnDetails].[TotalTaxAmount],
		[InclusiveTax] = [PurchaseReturnDetails].[InclusiveTax],
		[Total] = [PurchaseReturnDetails].[Total],
		[NetRate] = [PurchaseReturnDetails].[NetRate],
		[Remarks] = [PurchaseReturnDetails].[Remarks],
		[Status] = [PurchaseReturnDetails].[Status]
	FROM @PurchaseReturnDetails AS [PurchaseReturnDetails]
	WHERE [dbo].[PurchaseReturnDetail].[Id] = [PurchaseReturnDetails].[Id];

	INSERT INTO [dbo].[PurchaseReturnDetail]
	(
		[MasterId],
		[RawMaterialId],
		[Quantity],
		[UnitOfMeasurement],
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
		[RawMaterialId],
		[Quantity],
		[UnitOfMeasurement],
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
	FROM @PurchaseReturnDetails
	WHERE [Id] = 0;
END;
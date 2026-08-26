CREATE PROCEDURE [dbo].[Insert_PurchaseDetail_List]
	@PurchaseDetails [dbo].[PurchaseDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[PurchaseDetail]
	SET
		[MasterId] = [PurchaseDetails].[MasterId],
		[RawMaterialId] = [PurchaseDetails].[RawMaterialId],
		[Quantity] = [PurchaseDetails].[Quantity],
		[UnitOfMeasurement] = [PurchaseDetails].[UnitOfMeasurement],
		[Rate] = [PurchaseDetails].[Rate],
		[BaseTotal] = [PurchaseDetails].[BaseTotal],
		[DiscountPercent] = [PurchaseDetails].[DiscountPercent],
		[DiscountAmount] = [PurchaseDetails].[DiscountAmount],
		[AfterDiscount] = [PurchaseDetails].[AfterDiscount],
		[CGSTPercent] = [PurchaseDetails].[CGSTPercent],
		[CGSTAmount] = [PurchaseDetails].[CGSTAmount],
		[SGSTPercent] = [PurchaseDetails].[SGSTPercent],
		[SGSTAmount] = [PurchaseDetails].[SGSTAmount],
		[IGSTPercent] = [PurchaseDetails].[IGSTPercent],
		[IGSTAmount] = [PurchaseDetails].[IGSTAmount],
		[TotalTaxAmount] = [PurchaseDetails].[TotalTaxAmount],
		[InclusiveTax] = [PurchaseDetails].[InclusiveTax],
		[Total] = [PurchaseDetails].[Total],
		[NetRate] = [PurchaseDetails].[NetRate],
		[Remarks] = [PurchaseDetails].[Remarks],
		[Status] = [PurchaseDetails].[Status]
	FROM @PurchaseDetails AS [PurchaseDetails]
	WHERE [dbo].[PurchaseDetail].[Id] = [PurchaseDetails].[Id];

	INSERT INTO [dbo].[PurchaseDetail]
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
	FROM @PurchaseDetails
	WHERE [Id] = 0;
END;
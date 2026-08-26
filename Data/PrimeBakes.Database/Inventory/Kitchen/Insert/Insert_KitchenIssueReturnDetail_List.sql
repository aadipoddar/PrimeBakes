CREATE PROCEDURE [dbo].[Insert_KitchenIssueReturnDetail_List]
	@KitchenIssueReturnDetails [dbo].[KitchenIssueReturnDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[KitchenIssueReturnDetail]
	SET
		[MasterId] = [KitchenIssueReturnDetails].[MasterId],
		[RawMaterialId] = [KitchenIssueReturnDetails].[RawMaterialId],
		[Quantity] = [KitchenIssueReturnDetails].[Quantity],
		[UnitOfMeasurement] = [KitchenIssueReturnDetails].[UnitOfMeasurement],
		[Rate] = [KitchenIssueReturnDetails].[Rate],
		[Total] = [KitchenIssueReturnDetails].[Total],
		[Remarks] = [KitchenIssueReturnDetails].[Remarks],
		[Status] = [KitchenIssueReturnDetails].[Status]
	FROM @KitchenIssueReturnDetails AS [KitchenIssueReturnDetails]
	WHERE [dbo].[KitchenIssueReturnDetail].[Id] = [KitchenIssueReturnDetails].[Id];

	INSERT INTO [dbo].[KitchenIssueReturnDetail]
	(
		[MasterId],
		[RawMaterialId],
		[Quantity],
		[UnitOfMeasurement],
		[Rate],
		[Total],
		[Remarks],
		[Status]
	)
	SELECT
		[MasterId],
		[RawMaterialId],
		[Quantity],
		[UnitOfMeasurement],
		[Rate],
		[Total],
		[Remarks],
		[Status]
	FROM @KitchenIssueReturnDetails
	WHERE [Id] = 0;
END;
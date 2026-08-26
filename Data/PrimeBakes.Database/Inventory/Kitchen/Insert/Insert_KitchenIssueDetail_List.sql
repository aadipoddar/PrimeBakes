CREATE PROCEDURE [dbo].[Insert_KitchenIssueDetail_List]
	@KitchenIssueDetails [dbo].[KitchenIssueDetailType] READONLY
AS
BEGIN
	UPDATE [dbo].[KitchenIssueDetail]
	SET
		[MasterId] = [KitchenIssueDetails].[MasterId],
		[RawMaterialId] = [KitchenIssueDetails].[RawMaterialId],
		[Quantity] = [KitchenIssueDetails].[Quantity],
		[UnitOfMeasurement] = [KitchenIssueDetails].[UnitOfMeasurement],
		[Rate] = [KitchenIssueDetails].[Rate],
		[Total] = [KitchenIssueDetails].[Total],
		[Remarks] = [KitchenIssueDetails].[Remarks],
		[Status] = [KitchenIssueDetails].[Status]
	FROM @KitchenIssueDetails AS [KitchenIssueDetails]
	WHERE [dbo].[KitchenIssueDetail].[Id] = [KitchenIssueDetails].[Id];

	INSERT INTO [dbo].[KitchenIssueDetail]
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
	FROM @KitchenIssueDetails
	WHERE [Id] = 0;
END;
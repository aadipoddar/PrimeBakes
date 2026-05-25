CREATE PROCEDURE [dbo].[Load_RawMaterial_By_Party_PurchaseDateTime]
	@PartyId INT,
	@PurchaseDateTime DATETIME,
	@OnlyActive BIT
AS
BEGIN
	SELECT
		r.[Id],
		r.[Name],
		r.[Code],
		r.[RawMaterialCategoryId],

		ISNULL(
			CASE 
				WHEN @PartyId > 0 THEN
					(SELECT TOP 1 Rate FROM Purchase_Item_Overview pd
					 WHERE pd.Id = r.[Id]
					   AND pd.PartyId = @PartyId
					   AND pd.TransactionDateTime <= @PurchaseDateTime
					 ORDER BY pd.TransactionDateTime DESC)
				ELSE
					(SELECT TOP 1 Rate FROM Purchase_Item_Overview pd
					 WHERE pd.Id = r.[Id]
					   AND pd.TransactionDateTime <= @PurchaseDateTime
					 ORDER BY pd.TransactionDateTime DESC)
			END, r.[Rate]) AS [Rate],

		ISNULL(
			CASE 
				WHEN @PartyId > 0 THEN
					(SELECT TOP 1 UnitOfMeasurement FROM Purchase_Item_Overview pd
					 WHERE pd.Id = r.[Id]
					   AND pd.PartyId = @PartyId
					   AND pd.TransactionDateTime <= @PurchaseDateTime
					   ORDER BY pd.TransactionDateTime DESC)
				ELSE
					(SELECT TOP 1 UnitOfMeasurement FROM Purchase_Item_Overview pd
					 WHERE pd.Id = r.[Id]
					   AND pd.TransactionDateTime <= @PurchaseDateTime
					   ORDER BY pd.TransactionDateTime DESC)
			END, r.[UnitOfMeasurement]) AS [UnitOfMeasurement],

		r.[TaxId],
		r.[Status]

	FROM RawMaterial r
	WHERE (@OnlyActive = 0 OR r.[Status] = 1)
END
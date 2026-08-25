CREATE PROCEDURE [dbo].[Load_RawMaterial_By_Party_PurchaseDateTime]
	@PartyId INT,
	@PurchaseDateTime DATETIME,
	@OnlyActive BIT
AS
BEGIN

	SET NOCOUNT ON;

	WITH LastPurchase AS
	(
		SELECT
			pd.ItemId,
			pd.Rate,
			pd.UnitOfMeasurement,
			ROW_NUMBER() OVER (PARTITION BY pd.ItemId ORDER BY pd.TransactionDateTime DESC) AS Rn
		FROM Purchase_Item_Overview pd
		WHERE pd.TransactionDateTime <= @PurchaseDateTime
			AND (@PartyId <= 0 OR pd.PartyId = @PartyId)
	)
	SELECT
		r.Id,
		r.Name,
		r.Code,
		r.RawMaterialCategoryId,
		ISNULL(lp.Rate, r.Rate) AS Rate,
		ISNULL(lp.UnitOfMeasurement, r.UnitOfMeasurement) AS UnitOfMeasurement,
		r.TaxId,
		r.Status
	FROM RawMaterial r
	LEFT JOIN LastPurchase lp
		ON lp.ItemId = r.Id
		AND lp.Rn = 1
	WHERE (@OnlyActive = 0 OR r.Status = 1);

END

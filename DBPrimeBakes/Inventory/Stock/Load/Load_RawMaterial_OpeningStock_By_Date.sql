CREATE PROCEDURE [dbo].[Load_RawMaterial_OpeningStock_By_Date]
	@FromDate DATETIME
AS
BEGIN
	SELECT
		RawMaterialId,
		SUM(Quantity) AS Quantity
	FROM [RawMaterialStock] WITH (NOLOCK)
	WHERE [TransactionDateTime] < @FromDate
	GROUP BY RawMaterialId;
END

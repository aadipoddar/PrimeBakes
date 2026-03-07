CREATE PROCEDURE [dbo].[Load_RawMaterialStockSummary_By_Date]
	@FromDate DATETIME,
	@ToDate DATETIME
AS
BEGIN
	SET NOCOUNT ON;

	-- Keep transaction-type rules in one place to avoid repeating conditions.
	WITH StockBase AS (
		SELECT
			Id,
			RawMaterialId,
			Quantity,
			NetRate,
			[TransactionDateTime],
			CASE WHEN [Type] IN ('Purchase', 'SaleReturn', 'StockTransfer') THEN 1 ELSE 0 END AS IsPurchaseLike,
			CASE WHEN [Type] IN ('Sale', 'Bill', 'PurchaseReturn', 'StockTransfer') THEN 1 ELSE 0 END AS IsSaleLike
		FROM [RawMaterialStock] WITH (NOLOCK)
		WHERE [TransactionDateTime] <= @ToDate
	),
	
	-- Pre-calculate all stock aggregations in a single pass for each raw material
	StockAggregates AS (
		SELECT 
			RawMaterialId,
			-- Opening Stock: sum of all quantities before FromDate
			SUM(CASE WHEN [TransactionDateTime] < @FromDate THEN Quantity ELSE 0 END) AS OpeningStock,
			
			-- Purchase Stock: only purchase-like inflow types in date range
			SUM(CASE WHEN [TransactionDateTime] >= @FromDate AND [TransactionDateTime] <= @ToDate AND Quantity > 0
				AND IsPurchaseLike = 1
				THEN Quantity ELSE 0 END) AS PurchaseStock,
			
			-- Sale Stock: only sales-like outflow types in date range
			SUM(CASE WHEN [TransactionDateTime] >= @FromDate AND [TransactionDateTime] <= @ToDate AND Quantity < 0 
				AND IsSaleLike = 1
				THEN Quantity ELSE 0 END) AS SaleStock,
			
			-- Monthly Stock: sum of all quantities in date range
			SUM(CASE WHEN [TransactionDateTime] >= @FromDate AND [TransactionDateTime] <= @ToDate 
				THEN Quantity ELSE 0 END) AS MonthlyStock,
			
			-- Closing Stock: sum of all quantities up to ToDate
			SUM(CASE WHEN [TransactionDateTime] <= @ToDate THEN Quantity ELSE 0 END) AS ClosingStock
		FROM StockBase
		GROUP BY RawMaterialId
	),
	-- Calculate average prices for purchases in date range
	PriceAggregates AS (
		SELECT 
			RawMaterialId,
			AVG(CASE WHEN Quantity > 0 AND IsPurchaseLike = 1 THEN NetRate ELSE NULL END) AS AveragePrice
		FROM StockBase
		WHERE [TransactionDateTime] >= @FromDate 
			AND [TransactionDateTime] <= @ToDate
		GROUP BY RawMaterialId
	),
	-- Get last purchase price and date for each raw material in date range
	LastPurchaseInfo AS (
		SELECT 
			RawMaterialId,
			NetRate AS LastPurchasePrice,
			ROW_NUMBER() OVER (PARTITION BY RawMaterialId ORDER BY [TransactionDateTime] DESC, Id DESC) AS RowNum
		FROM StockBase
		WHERE [TransactionDateTime] >= @FromDate 
			AND [TransactionDateTime] <= @ToDate
			AND Quantity > 0
			AND IsPurchaseLike = 1
	)
	-- Final select combining all pre-calculated data
	SELECT
		sa.RawMaterialId,
		r.[Name] RawMaterialName,
		r.Code RawMaterialCode,
		r.RawMaterialCategoryId,
		rc.[Name] RawMaterialCategoryName,
		r.UnitOfMeasurement,
		
		ISNULL(sa.OpeningStock, 0) AS OpeningStock,
		ISNULL(sa.PurchaseStock, 0) AS PurchaseStock,
		ISNULL(sa.SaleStock, 0) AS SaleStock,
		ISNULL(sa.MonthlyStock, 0) AS MonthlyStock,
		ISNULL(sa.ClosingStock, 0) AS ClosingStock,
		
		r.Rate,
		ISNULL(r.Rate * sa.ClosingStock, 0) AS ClosingValue,
		
		ISNULL(pa.AveragePrice, 0) AS AveragePrice,
		ISNULL(pa.AveragePrice * sa.ClosingStock, 0) AS WeightedAverageValue,
		
		ISNULL(lpi.LastPurchasePrice, 0) AS LastPurchasePrice,
		ISNULL(lpi.LastPurchasePrice * sa.ClosingStock, 0) AS LastPurchaseValue
		
	FROM StockAggregates sa
	
	LEFT JOIN dbo.RawMaterial r WITH (NOLOCK)
		ON r.Id = sa.RawMaterialId
	
	LEFT JOIN dbo.RawMaterialCategory rc WITH (NOLOCK) 
		ON rc.Id = r.RawMaterialCategoryId
		
	LEFT JOIN PriceAggregates pa 
		ON pa.RawMaterialId = sa.RawMaterialId
		
	LEFT JOIN LastPurchaseInfo lpi 
		ON lpi.RawMaterialId = sa.RawMaterialId AND lpi.RowNum = 1
		
	WHERE sa.OpeningStock != 0 
		OR sa.PurchaseStock != 0 
		OR sa.SaleStock != 0 
		OR sa.ClosingStock != 0;
END
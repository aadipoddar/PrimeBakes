namespace PrimeBakes.Models.Inventory.Stock;

public sealed record ProductStockAdjustmentRequest(
	DateTime TransactionDateTime,
	int LocationId,
	List<ProductStockAdjustmentCartModel> Cart);

public sealed record RawMaterialStockAdjustmentRequest(
	DateTime TransactionDateTime,
	List<RawMaterialStockAdjustmentCartModel> Cart);

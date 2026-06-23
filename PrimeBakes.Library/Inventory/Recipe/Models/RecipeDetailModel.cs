namespace PrimeBakes.Library.Inventory.Recipe.Models;

public class RecipeDetailModel
{
	public int Id { get; set; }
	public int MasterId { get; set; }
	public int RawMaterialId { get; set; }
	public decimal Quantity { get; set; }
	public bool Status { get; set; }
}

public class RecipeItemCartModel
{
	public int ItemId { get; set; }
	public string ItemName { get; set; }
	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal Amount { get; set; }
	public decimal PerUnit { get; set; }
}

public class RecipeItemOverviewModel
{
	public int Id { get; set; }
	public int ItemId { get; set; }
	public string ItemName { get; set; }
	public string ItemCode { get; set; }
	public int ItemCategoryId { get; set; }
	public string ItemCategoryName { get; set; }
	public string UnitOfMeasurement { get; set; }

	public decimal Quantity { get; set; }
	public decimal Rate { get; set; }
	public decimal Amount { get; set; }
	public decimal PerUnit { get; set; }

	public int MasterId { get; set; }
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public decimal RecipeQuantity { get; set; }
	public bool Deduct { get; set; }
	public DateOnly FromDate { get; set; }

	public bool MasterStatus { get; set; }
}
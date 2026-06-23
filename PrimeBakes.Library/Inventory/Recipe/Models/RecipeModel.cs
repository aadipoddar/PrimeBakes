namespace PrimeBakes.Library.Inventory.Recipe.Models;

public class RecipeModel
{
	public int Id { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public bool Deduct { get; set; }
	public DateOnly FromDate { get; set; }
	public bool Status { get; set; }
}

public class RecipeOverviewModel
{
	public int Id { get; set; }
	public int ProductId { get; set; }
	public string ProductName { get; set; }
	public decimal Quantity { get; set; }
	public bool Deduct { get; set; }
	public int ItemCount { get; set; }
	public decimal TotalCost { get; set; }
	public decimal PerUnitCost { get; set; }
	public DateOnly FromDate { get; set; }
	public bool Status { get; set; }
}
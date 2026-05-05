namespace PrimeBakesLibrary.Models.Inventory;

public class RecipeModel
{
    public int Id { get; set; }
    public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public bool Deduct { get; set; }
	public bool Status { get; set; }
}

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
}
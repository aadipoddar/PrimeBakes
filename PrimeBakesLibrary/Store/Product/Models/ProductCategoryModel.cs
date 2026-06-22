namespace PrimeBakesLibrary.Store.Product.Models;

public class ProductCategoryModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public bool ShowInMenu { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

namespace PrimeBakesLibrary.Models;

public class ItemCategoryModel
{
	public int Id { get; set; }
	public string Code { get; set; }
	public string Name { get; set; }
	public bool Status { get; set; }

	public string DisplayName => $"{Name} ({Code})";
}
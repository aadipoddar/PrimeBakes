namespace PrimeBakesLibrary.Models.Restuarant.Dining;

public class DiningAreaModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int LocationId { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

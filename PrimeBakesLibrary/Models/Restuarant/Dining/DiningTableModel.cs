namespace PrimeBakesLibrary.Models.Restuarant.Dining;

public class DiningTableModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int DiningAreaId { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

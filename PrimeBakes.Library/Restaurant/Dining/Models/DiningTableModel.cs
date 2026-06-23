namespace PrimeBakes.Library.Restaurant.Dining.Models;

public class DiningTableModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public int DiningAreaId { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
	public string? LayoutJson { get; set; }
}

public class DiningTableLayout
{
	public double X { get; set; }
	public double Y { get; set; }
	public double W { get; set; }
	public double H { get; set; }
	public string Shape { get; set; }
}

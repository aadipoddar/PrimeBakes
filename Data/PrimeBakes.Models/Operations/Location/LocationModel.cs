namespace PrimeBakes.Models.Operations.Location;

public class LocationModel
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string Code { get; set; }
	public decimal Discount { get; set; }
	public int LedgerId { get; set; }
	public bool COCO { get; set; }
	public bool FOFO { get; set; }
	public bool UseLocationRateOnSale { get; set; }
	public decimal? Latitude { get; set; }
	public decimal? Longitude { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}
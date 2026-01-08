namespace PrimeBakesLibrary.Models.Operations;

public class LocationModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public decimal Discount { get; set; }
    public int LedgerId { get; set; }
    public string? Remarks { get; set; }
    public bool Status { get; set; }
}
namespace PrimeBakes.Models.Operations.Analysis;

public class AnalysisMonthlyTrendModel
{
	public int Year { get; set; }
	public int Month { get; set; }
	public decimal Revenue { get; set; }
	public decimal Purchase { get; set; }
}

public class AnalysisTopProductModel
{
	public string ItemName { get; set; }
	public decimal Quantity { get; set; }
	public decimal Amount { get; set; }
}

public class AnalysisTopRawMaterialModel
{
	public string ItemName { get; set; }
	public string UnitOfMeasurement { get; set; }
	public decimal Quantity { get; set; }
	public decimal Amount { get; set; }
}

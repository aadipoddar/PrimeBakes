namespace PrimeBakes.Library.Inventory.Kitchen.Models;

public class KitchenProductionModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public int KitchenId { get; set; }
	public int TotalItems { get; set; }
	public decimal TotalQuantity { get; set; }
	public decimal TotalAmount { get; set; }
	public string? Remarks { get; set; }
	public int CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public bool Status { get; set; }
	public int? LastModifiedBy { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }
}

public class KitchenProductionOverviewModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public string FinancialYear { get; set; }

	public int KitchenId { get; set; }
	public string KitchenName { get; set; }

	public int TotalItems { get; set; }
	public decimal TotalQuantity { get; set; }
	public decimal TotalAmount { get; set; }

	public string Remarks { get; set; }
	public int CreatedBy { get; set; }
	public string CreatedByName { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public int? LastModifiedBy { get; set; }
	public string LastModifiedByUserName { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string LastModifiedFromPlatform { get; set; }
	public bool Status { get; set; }
}
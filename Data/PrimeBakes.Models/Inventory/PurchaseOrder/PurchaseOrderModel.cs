namespace PrimeBakes.Models.Inventory.PurchaseOrder;

public class PurchaseOrderModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public int PartyId { get; set; }
	public int? PurchaseId { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public DateOnly? ExpectedDeliveryDate { get; set; }
	public int FinancialYearId { get; set; }

	public int TotalItems { get; set; }
	public decimal TotalQuantity { get; set; }

	public string? Remarks { get; set; }
	public int CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }
	public string? CreatedFormFactor { get; set; }
	public string? CreatedPlatform { get; set; }
	public decimal? CreatedLatitude { get; set; }
	public decimal? CreatedLongitude { get; set; }
	public bool Status { get; set; }
	public int? LastModifiedBy { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFormFactor { get; set; }
	public string? LastModifiedPlatform { get; set; }
	public decimal? LastModifiedLatitude { get; set; }
	public decimal? LastModifiedLongitude { get; set; }
}

public class PurchaseOrderOverviewModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }
	public int PartyId { get; set; }
	public string PartyName { get; set; }

	public int? PurchaseId { get; set; }
	public string? PurchaseTransactionNo { get; set; }
	public DateTime? PurchaseDateTime { get; set; }

	public DateTime TransactionDateTime { get; set; }
	public DateOnly? ExpectedDeliveryDate { get; set; }
	public int FinancialYearId { get; set; }
	public string FinancialYear { get; set; }

	public int TotalItems { get; set; }
	public decimal TotalQuantity { get; set; }

	public string? Remarks { get; set; }
	public int CreatedBy { get; set; }
	public string CreatedByName { get; set; }
	public DateTime CreatedAt { get; set; }
	public string? CreatedFormFactor { get; set; }
	public string? CreatedPlatform { get; set; }
	public decimal? CreatedLatitude { get; set; }
	public decimal? CreatedLongitude { get; set; }
	public int? LastModifiedBy { get; set; }
	public string? LastModifiedByUserName { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFormFactor { get; set; }
	public string? LastModifiedPlatform { get; set; }
	public decimal? LastModifiedLatitude { get; set; }
	public decimal? LastModifiedLongitude { get; set; }
	public bool Status { get; set; }
}

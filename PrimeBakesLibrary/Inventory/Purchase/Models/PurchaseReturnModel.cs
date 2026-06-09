namespace PrimeBakesLibrary.Inventory.Purchase.Models;

public class PurchaseReturnModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public string? ChallanNo { get; set; }
	public int CompanyId { get; set; }
	public int PartyId { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }

	public int TotalItems { get; set; }
	public decimal TotalQuantity { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal ItemDiscountAmount { get; set; }
	public decimal TotalAfterItemDiscount { get; set; }
	public decimal TotalInclusiveTaxAmount { get; set; }
	public decimal TotalExtraTaxAmount { get; set; }
	public decimal TotalAfterTax { get; set; }
	public decimal OtherChargesPercent { get; set; }
	public decimal OtherChargesAmount { get; set; }
	public decimal CashDiscountPercent { get; set; }
	public decimal CashDiscountAmount { get; set; }
	public decimal RoundOffAmount { get; set; }
	public decimal TotalAmount { get; set; }

	public string? Remarks { get; set; }
	public string? DocumentUrl { get; set; }
	public int? FinancialAccountingId { get; set; }
	public int CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public bool Status { get; set; }
	public int? LastModifiedBy { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }
}

public class PurchaseReturnOverviewModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public string? ChallanNo { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }
	public int PartyId { get; set; }
	public string PartyName { get; set; }
	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public string FinancialYear { get; set; }

	public int TotalItems { get; set; }
	public decimal TotalQuantity { get; set; }
	public decimal BaseTotal { get; set; }
	public decimal ItemDiscountAmount { get; set; }
	public decimal TotalAfterItemDiscount { get; set; }
	public decimal TotalInclusiveTaxAmount { get; set; }
	public decimal TotalExtraTaxAmount { get; set; }
	public decimal TotalAfterTax { get; set; }

	public decimal OtherChargesPercent { get; set; }
	public decimal OtherChargesAmount { get; set; }
	public decimal CashDiscountPercent { get; set; }
	public decimal CashDiscountAmount { get; set; }

	public decimal RoundOffAmount { get; set; }
	public decimal TotalAmount { get; set; }

	public string? Remarks { get; set; }
	public string? DocumentUrl { get; set; }
	public int? FinancialAccountingId { get; set; }
	public string? FinancialAccountingTransactionNo { get; set; }
	public int CreatedBy { get; set; }
	public string CreatedByName { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public int? LastModifiedBy { get; set; }
	public string? LastModifiedByUserName { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }
	public bool Status { get; set; }
}
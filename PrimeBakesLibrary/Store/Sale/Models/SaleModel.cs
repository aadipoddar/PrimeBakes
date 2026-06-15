namespace PrimeBakesLibrary.Store.Sale.Models;

public class SaleModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public int LocationId { get; set; }
	public int? PartyId { get; set; }
	public int? CustomerId { get; set; }
	public int? OrderId { get; set; }
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
	public decimal DiscountPercent { get; set; }
	public decimal DiscountAmount { get; set; }
	public decimal RoundOffAmount { get; set; }
	public decimal TotalAmount { get; set; }
	public decimal Cash { get; set; }
	public decimal Card { get; set; }
	public decimal UPI { get; set; }
	public decimal Credit { get; set; }
	public string? Remarks { get; set; }
	public int? FinancialAccountingId { get; set; }
	public int CreatedBy { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public bool Status { get; set; }
	public int? LastModifiedBy { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }
}

public class SaleOverviewModel
{
	public int Id { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }
	public int LocationId { get; set; }
	public string LocationName { get; set; }

	public int? PartyId { get; set; }
	public string? PartyName { get; set; }
	public int? CustomerId { get; set; }
	public string? CustomerName { get; set; }

	public int? OrderId { get; set; }
	public string? OrderTransactionNo { get; set; }
	public DateTime? OrderDateTime { get; set; }

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
	public decimal DiscountPercent { get; set; }
	public decimal DiscountAmount { get; set; }

	public decimal RoundOffAmount { get; set; }
	public decimal TotalAmount { get; set; }

	public decimal Cash { get; set; }
	public decimal Card { get; set; }
	public decimal UPI { get; set; }
	public decimal Credit { get; set; }

	public string? PaymentModes { get; set; }

	public string? Remarks { get; set; }
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

public class OutletSummaryModel
{
	public int LocationId { get; set; }
	public string LocationName { get; set; }
	public decimal Purchase { get; set; }
	public decimal PurchaseReturn { get; set; }
	public decimal KitchenIssue { get; set; }
	public decimal KitchenProduction { get; set; }
	public decimal Sale { get; set; }
	public decimal SaleReturn { get; set; }
}
namespace PrimeBakes.Models.Store.Order;

public class OrderDetailModel
{
	public int Id { get; set; }
	public int MasterId { get; set; }
	public int ProductId { get; set; }
	public decimal Quantity { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public class OrderItemCartModel
{
	public int ItemCategoryId { get; set; }
	public int ItemId { get; set; }
	public string ItemName { get; set; }
	public decimal Quantity { get; set; }
	public string? Remarks { get; set; }
}

public class OrderItemOverviewModel
{
	public int Id { get; set; }
	public int ItemId { get; set; }
	public string ItemName { get; set; }
	public string ItemCode { get; set; }
	public int ItemCategoryId { get; set; }
	public string ItemCategoryName { get; set; }

	public decimal Quantity { get; set; }
	public string? ItemRemarks { get; set; }

	public int MasterId { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }
	public int LocationId { get; set; }
	public string LocationName { get; set; }

	public int? SaleId { get; set; }
	public string? SaleTransactionNo { get; set; }
	public DateTime? SaleDateTime { get; set; }

	public DateTime TransactionDateTime { get; set; }
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
	public double? CreatedLocationOffset { get; set; }
	public double? CreatedUserOffset { get; set; }
	public double? LastModifiedLocationOffset { get; set; }
	public double? LastModifiedUserOffset { get; set; }

	public bool MasterStatus { get; set; }
}
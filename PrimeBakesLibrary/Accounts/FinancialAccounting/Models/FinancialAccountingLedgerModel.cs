namespace PrimeBakesLibrary.Accounts.FinancialAccounting.Models;

public class FinancialAccountingLedgerModel
{
	public int Id { get; set; }
	public int MasterId { get; set; }
	public int LedgerId { get; set; }
	public int? ReferenceId { get; set; }
	public string? ReferenceType { get; set; }
	public string? ReferenceNo { get; set; }
	public decimal? Debit { get; set; }
	public decimal? Credit { get; set; }
	public string? InstrumentNo { get; set; }
	public DateTime? InstrumentDate { get; set; }
	public DateTime? ClearingDate { get; set; }
	public string? Remarks { get; set; }
	public bool Status { get; set; }
}

public class FinancialAccountingLedgerCartModel
{
	public int LedgerId { get; set; }
	public string LedgerName { get; set; }
	public string? ReferenceType { get; set; }
	public int? ReferenceId { get; set; }
	public string? ReferenceNo { get; set; }
	public decimal? Debit { get; set; }
	public decimal? Credit { get; set; }
	public string? InstrumentNo { get; set; }
	public DateTime? InstrumentDate { get; set; }
	public string? Remarks { get; set; }
}

public class FinancialAccountingLedgerOverviewModel
{
	public int Id { get; set; }
	public int LedgerId { get; set; }
	public string LedgerName { get; set; }
	public string LedgerCode { get; set; }

	public int AccountTypeId { get; set; }
	public string AccountTypeName { get; set; }
	public int GroupId { get; set; }
	public string GroupName { get; set; }

	public int? LedgerReferenceId { get; set; }
	public string? LedgerReferenceType { get; set; }
	public string? LedgerReferenceNo { get; set; }

	public decimal? Debit { get; set; }
	public decimal? Credit { get; set; }

	public string? InstrumentNo { get; set; }
	public DateTime? InstrumentDate { get; set; }
	public DateTime? ClearingDate { get; set; }
	public bool? Reconciled { get; set; }

	public string? LedgerRemarks { get; set; }

	public int MasterId { get; set; }
	public string TransactionNo { get; set; }
	public int CompanyId { get; set; }
	public string CompanyName { get; set; }
	public int VoucherId { get; set; }
	public string VoucherName { get; set; }

	public int? ReferenceId { get; set; }
	public string? ReferenceNo { get; set; }

	public DateTime TransactionDateTime { get; set; }
	public int FinancialYearId { get; set; }
	public string FinancialYear { get; set; }

	public int TotalLedgers { get; set; }
	public int TotalDebitLedgers { get; set; }
	public int TotalCreditLedgers { get; set; }
	public decimal TotalCreditAmount { get; set; }
	public decimal TotalDebitAmount { get; set; }
	public decimal TotalAmount { get; set; }

	public string? Remarks { get; set; }
	public int CreatedBy { get; set; }
	public string CreatedByName { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedFromPlatform { get; set; }
	public int? LastModifiedBy { get; set; }
	public string? LastModifiedByUserName { get; set; }
	public DateTime? LastModifiedAt { get; set; }
	public string? LastModifiedFromPlatform { get; set; }
	public bool MasterStatus { get; set; }

	// Display text for exports; mirrors the grid's Reconciled column (blank when not an instrument line)
	public string ReconciledStatus => Reconciled is null ? string.Empty : Reconciled.Value ? "Reconciled" : "Unreconciled";

	public FinancialAccountingLedgerOverviewModel Clone() => (FinancialAccountingLedgerOverviewModel)MemberwiseClone();
}

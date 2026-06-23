namespace PrimeBakes.Library.Accounts.FinancialAccounting.Models;

public class TrialBalanceModel
{
    public int LedgerId { get; set; }
    public string LedgerCode { get; set; }
    public string LedgerName { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; }
    public string NatureName { get; set; }
    public int AccountTypeId { get; set; }
    public string AccountTypeName { get; set; }

    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal ClosingDebit { get; set; }
    public decimal ClosingCredit { get; set; }
    public decimal ClosingBalance { get; set; }
}
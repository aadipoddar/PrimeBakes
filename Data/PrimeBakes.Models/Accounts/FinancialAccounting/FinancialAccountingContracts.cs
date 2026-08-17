using PrimeBakes.Models.Accounts.Masters;

namespace PrimeBakes.Models.Accounts.FinancialAccounting;

public sealed record FinancialAccountingSaveRequest(
	FinancialAccountingModel Accounting,
	List<FinancialAccountingLedgerModel> Ledgers,
	bool Recover);

public sealed record FinancialAccountingInvoiceBundle(
	FinancialAccountingOverviewModel Transaction,
	List<FinancialAccountingLedgerOverviewModel> Details,
	CompanyModel Company,
	DateTime CurrentDateTime);

public static class FinancialAccountingCartExtensions
{
	public static List<FinancialAccountingLedgerModel> ConvertCartToDetails(this List<FinancialAccountingLedgerCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new FinancialAccountingLedgerModel
		{
			Id = 0,
			MasterId = masterId,
			LedgerId = item.LedgerId,
			Credit = item.Credit,
			Debit = item.Debit,
			ReferenceType = item.ReferenceType,
			ReferenceId = item.ReferenceId,
			ReferenceNo = item.ReferenceNo,
			InstrumentNo = item.InstrumentNo,
			InstrumentDate = item.InstrumentDate,
			Remarks = item.Remarks,
			Status = true
		})];
}

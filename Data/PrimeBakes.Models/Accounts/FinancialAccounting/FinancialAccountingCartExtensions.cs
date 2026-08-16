namespace PrimeBakes.Models.Accounts.FinancialAccounting;

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

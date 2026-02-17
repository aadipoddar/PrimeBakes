using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Exporting.Restaurant.Bill;
using PrimeBakesLibrary.Exporting.Utils;
using PrimeBakesLibrary.Models.Restuarant.Bill;

namespace PrimeBakesLibrary.Data.Restaurant.Bill;

public static class BillData
{
	private static async Task<int> InsertBill(BillModel bill, SqlDataAccessTransaction? sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertBill, bill, sqlDataAccessTransaction)).FirstOrDefault();

	private static async Task<int> InsertBillDetail(BillDetailModel billDetail, SqlDataAccessTransaction? sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertBillDetail, billDetail, sqlDataAccessTransaction)).FirstOrDefault();

	private static async Task DeleteBillDetailById(int Id, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.SaveData(StoredProcedureNames.DeleteBillDetailById, new { Id }, sqlDataAccessTransaction);

	public static async Task<List<BillModel>> LoadRunningBillByLocationId(int LocationId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<BillModel, dynamic>(StoredProcedureNames.LoadRunningBillByLocationId, new { LocationId }, sqlDataAccessTransaction);

	public static async Task DeleteTransaction(BillModel bill)
	{
		await FinancialYearData.ValidateFinancialYear(bill.TransactionDateTime);

		using SqlDataAccessTransaction sqlDataAccessTransaction = new();

		try
		{
			sqlDataAccessTransaction.StartTransaction();

			bill.Status = false;
			await InsertBill(bill, sqlDataAccessTransaction);

			sqlDataAccessTransaction.CommitTransaction();

			await BillNotify.Notify(bill.Id, NotifyType.Deleted);
		}
		catch
		{
			sqlDataAccessTransaction.RollbackTransaction();
			throw;
		}
	}

	public static async Task RecoverTransaction(BillModel bill)
	{
		bill.Status = true;
		var transactionDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, bill.Id);

		await SaveTransaction(bill, null, transactionDetails, false);
		await BillNotify.Notify(bill.Id, NotifyType.Recovered);
	}

	private static List<BillDetailModel> ConvertCartToDetails(List<BillItemCartModel> cart, int masterId) =>
		[.. cart.Select(item => new BillDetailModel
		{
			Id = 0,
			MasterId = masterId,
			ProductId = item.ItemId,
			Quantity = item.Quantity,
			Rate = item.Rate,
			BaseTotal = item.BaseTotal,
			DiscountPercent = item.DiscountPercent,
			DiscountAmount = item.DiscountAmount,
			AfterDiscount = item.AfterDiscount,
			CGSTPercent = item.CGSTPercent,
			CGSTAmount = item.CGSTAmount,
			SGSTPercent = item.SGSTPercent,
			SGSTAmount = item.SGSTAmount,
			IGSTPercent = item.IGSTPercent,
			IGSTAmount = item.IGSTAmount,
			TotalTaxAmount = item.TotalTaxAmount,
			InclusiveTax = item.InclusiveTax,
			NetRate = item.NetRate,
			Total = item.Total,
			Remarks = item.Remarks,
			KOTPrint = item.KOTPrint,
			Status = true
		})];

	public static async Task<int> SaveTransaction(BillModel bill, List<BillItemCartModel> cart, List<BillDetailModel> billDetails = null, bool showNotification = true, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = bill.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = null;
			if (update && !bill.Running)
				previousInvoice = await BillInvoiceExport.ExportInvoice(bill.Id, InvoiceExportType.PDF);

			using SqlDataAccessTransaction newSqlDataAccessTransaction = new();

			try
			{
				newSqlDataAccessTransaction.StartTransaction();
				bill.Id = await SaveTransaction(bill, cart, billDetails, showNotification, newSqlDataAccessTransaction);
				newSqlDataAccessTransaction.CommitTransaction();
			}
			catch
			{
				newSqlDataAccessTransaction.RollbackTransaction();
				throw;
			}

			if (showNotification && !bill.Running)
				await BillNotify.Notify(bill.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return bill.Id;
		}

		var existingBill = bill;
		var previousRunning = true;

		if (update)
		{
			existingBill = await CommonData.LoadTableDataById<BillModel>(TableNames.Bill, bill.Id, sqlDataAccessTransaction);
			await FinancialYearData.ValidateFinancialYear(existingBill.TransactionDateTime, sqlDataAccessTransaction);
			bill.TransactionNo = existingBill.TransactionNo;
			previousRunning = existingBill.Running;
		}
		else
			bill.TransactionNo = await GenerateCodes.GenerateBillTransactionNo(bill, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(bill.TransactionDateTime, sqlDataAccessTransaction);

		bill.Id = await InsertBill(bill, sqlDataAccessTransaction);
		billDetails ??= ConvertCartToDetails(cart, bill.Id);
		await SaveTransactionDetail(bill, billDetails, update, previousRunning, sqlDataAccessTransaction);

		return bill.Id;
	}

	public static async Task MarkKOTAsPrinted(int billId)
	{
		var billDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, billId);

		foreach (var detail in billDetails.Where(d => d.KOTPrint))
		{
			detail.KOTPrint = false;
			await InsertBillDetail(detail);
		}
	}

	private static async Task SaveTransactionDetail(BillModel bill, List<BillDetailModel> billDetails, bool update, bool previousRunning, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (billDetails is null || billDetails.Count != bill.TotalItems || billDetails.Sum(d => d.Quantity) != bill.TotalQuantity)
			throw new InvalidOperationException("Bill details do not match the transaction summary.");

		if (billDetails.Any(d => !d.Status))
			throw new InvalidOperationException("Bill detail items must be active.");

		if (update)
		{
			var existingBillDetails = await CommonData.LoadTableDataByMasterId<BillDetailModel>(TableNames.BillDetail, bill.Id, sqlDataAccessTransaction);
			foreach (var item in existingBillDetails)
			{
				if (bill.Running || previousRunning)
					await DeleteBillDetailById(item.Id, sqlDataAccessTransaction);

				else
				{
					item.Status = false;
					await InsertBillDetail(item, sqlDataAccessTransaction);
				}
			}
		}

		foreach (var item in billDetails)
		{
			item.MasterId = bill.Id;
			var id = await InsertBillDetail(item, sqlDataAccessTransaction);

			if (id <= 0)
				throw new InvalidOperationException("Failed to save bill detail item.");
		}
	}
}
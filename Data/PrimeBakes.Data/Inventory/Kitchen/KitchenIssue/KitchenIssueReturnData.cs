using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Common;
using PrimeBakes.Data.Inventory.Stock;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Exports.Inventory.Kitchen;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;
using PrimeBakes.Models.Inventory.Stock;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Inventory.Kitchen.KitchenIssue;

public static class KitchenIssueReturnData
{

	private static async Task<int> InsertKitchenIssueReturn(KitchenIssueReturnModel kitchenIssueReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertKitchenIssueReturn, kitchenIssueReturn, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Kitchen Issue Return.");

	private static async Task<int> InsertKitchenIssueReturnDetail(KitchenIssueReturnDetailModel kitchenIssueReturnDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertKitchenIssueReturnDetail, kitchenIssueReturnDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Kitchen Issue Return Detail.");

	public static async Task<KitchenIssueReturnInvoiceBundle> LoadInvoiceBundle(int transactionId)
	{
		var transaction = await CommonData.LoadTableDataById<KitchenIssueReturnOverviewModel>(InventoryNames.KitchenIssueReturnOverview, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueReturnItemOverviewModel>(InventoryNames.KitchenIssueReturnItemOverview, transaction.Id);
		transactionDetails = [.. transactionDetails.OrderBy(detail => detail.ItemName)];
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, transaction.CompanyId);
		var kitchen = await CommonData.LoadTableDataById<KitchenModel>(InventoryNames.Kitchen, transaction.KitchenId);
		if (company is null || kitchen is null)
			throw new InvalidOperationException("Company or kitchen information is missing.");

		return new(transaction, transactionDetails, company, kitchen, await CommonData.LoadCurrentDateTime());
	}

	#region Delete
	public static async Task DeleteTransaction(KitchenIssueReturnModel kitchenIssueReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(kitchenIssueReturn, transaction));
			await KitchenIssueReturnNotify.Notify(kitchenIssueReturn.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(kitchenIssueReturn.TransactionDateTime, sqlDataAccessTransaction);

		kitchenIssueReturn.Status = false;
		await InsertKitchenIssueReturn(kitchenIssueReturn, sqlDataAccessTransaction);
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(kitchenIssueReturn.TransactionNo, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = InventoryNames.KitchenIssueReturn,
			RecordNo = kitchenIssueReturn.TransactionNo,
			CreatedBy = kitchenIssueReturn.LastModifiedBy.Value,
			CreatedFromPlatform = kitchenIssueReturn.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}

	public static async Task RecoverTransaction(KitchenIssueReturnModel kitchenIssueReturn)
	{
		kitchenIssueReturn.Status = true;
		var kitchenIssueReturnDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueReturnDetailModel>(InventoryNames.KitchenIssueReturnDetail, kitchenIssueReturn.Id);
		await SaveTransaction(kitchenIssueReturn, kitchenIssueReturnDetails, true);

		await KitchenIssueReturnNotify.Notify(kitchenIssueReturn.Id, NotifyType.Recovered);
	}
	#endregion

	#region Save
	private static async Task<KitchenIssueReturnModel> ValidateTransaction(KitchenIssueReturnModel kitchenIssueReturn, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		kitchenIssueReturn.Remarks = string.IsNullOrWhiteSpace(kitchenIssueReturn.Remarks) ? null : kitchenIssueReturn.Remarks.Trim();

		if (kitchenIssueReturn.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (kitchenIssueReturn.KitchenId <= 0)
			throw new InvalidOperationException("Please select a kitchen for the transaction.");

		if (kitchenIssueReturn.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (kitchenIssueReturn.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (kitchenIssueReturn.TotalAmount < 0)
			throw new InvalidOperationException("The total amount of the transaction cannot be negative.");

		if (!update)
			kitchenIssueReturn.TransactionNo = await GenerateCodes.GenerateKitchenIssueReturnTransactionNo(kitchenIssueReturn, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(kitchenIssueReturn.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingKitchenIssueReturn = await CommonData.LoadTableDataById<KitchenIssueReturnModel>(InventoryNames.KitchenIssueReturn, kitchenIssueReturn.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingKitchenIssueReturn.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, kitchenIssueReturn.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin || user.LocationId != 1)
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			kitchenIssueReturn.TransactionNo = existingKitchenIssueReturn.TransactionNo;
		}

		return kitchenIssueReturn;
	}

	private static void ValidateItemDetails(KitchenIssueReturnModel kitchenIssueReturn, List<KitchenIssueReturnDetailModel> kitchenIssueReturnDetails)
	{
		if (kitchenIssueReturnDetails is null || kitchenIssueReturnDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (kitchenIssueReturnDetails.Count != kitchenIssueReturn.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (kitchenIssueReturnDetails.Sum(ed => ed.Quantity) != kitchenIssueReturn.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		foreach (var item in kitchenIssueReturnDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
	}

	public static async Task<int> SaveTransaction(
		KitchenIssueReturnModel kitchenIssueReturn,
		List<KitchenIssueReturnDetailModel> kitchenIssueReturnDetails,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = kitchenIssueReturn.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? KitchenIssueReturnInvoiceExport.ExportInvoice(await LoadInvoiceBundle(kitchenIssueReturn.Id), InvoiceExportType.PDF) : null;

			kitchenIssueReturn.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(kitchenIssueReturn, kitchenIssueReturnDetails, recover, transaction));

			if (!recover)
				await KitchenIssueReturnNotify.Notify(kitchenIssueReturn.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return kitchenIssueReturn.Id;
		}

		kitchenIssueReturn = await ValidateTransaction(kitchenIssueReturn, update, sqlDataAccessTransaction);
		ValidateItemDetails(kitchenIssueReturn, kitchenIssueReturnDetails);

		var previousKitchenIssueReturn = update && !recover ? await CommonData.LoadTableDataById<KitchenIssueReturnOverviewModel>(InventoryNames.KitchenIssueReturnOverview, kitchenIssueReturn.Id, sqlDataAccessTransaction) : new();
		var previousKitchenIssueReturnDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<KitchenIssueReturnItemOverviewModel>(InventoryNames.KitchenIssueReturnItemOverview, kitchenIssueReturn.Id, sqlDataAccessTransaction) : [];

		kitchenIssueReturn.Id = await InsertKitchenIssueReturn(kitchenIssueReturn, sqlDataAccessTransaction);
		await SaveTransactionDetail(kitchenIssueReturn, kitchenIssueReturnDetails, update, sqlDataAccessTransaction);
		await SaveRawMaterialStock(kitchenIssueReturn, kitchenIssueReturnDetails, sqlDataAccessTransaction);
		await SaveAuditTrail(kitchenIssueReturn, update, recover, previousKitchenIssueReturn, previousKitchenIssueReturnDetails, sqlDataAccessTransaction);

		return kitchenIssueReturn.Id;
	}

	private static async Task SaveTransactionDetail(KitchenIssueReturnModel kitchenIssueReturn, List<KitchenIssueReturnDetailModel> kitchenIssueReturnDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingKitchenIssueReturnDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueReturnDetailModel>(InventoryNames.KitchenIssueReturnDetail, kitchenIssueReturn.Id, sqlDataAccessTransaction);
			foreach (var item in existingKitchenIssueReturnDetails)
			{
				item.Status = false;
				await InsertKitchenIssueReturnDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in kitchenIssueReturnDetails)
		{
			item.MasterId = kitchenIssueReturn.Id;
			await InsertKitchenIssueReturnDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveRawMaterialStock(KitchenIssueReturnModel kitchenIssueReturn, List<KitchenIssueReturnDetailModel> kitchenIssueReturnDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await RawMaterialStockData.DeleteRawMaterialStockByTransactionNo(kitchenIssueReturn.TransactionNo, sqlDataAccessTransaction);

		foreach (var item in kitchenIssueReturnDetails)
			await RawMaterialStockData.InsertRawMaterialStock(new()
			{
				Id = 0,
				RawMaterialId = item.RawMaterialId,
				Quantity = item.Quantity,
				NetRate = item.Rate,
				Type = nameof(StockType.KitchenIssueReturn),
				TransactionId = kitchenIssueReturn.Id,
				TransactionNo = kitchenIssueReturn.TransactionNo,
				TransactionDateTime = kitchenIssueReturn.TransactionDateTime
			}, sqlDataAccessTransaction);
	}

	private static async Task SaveAuditTrail(
		KitchenIssueReturnModel kitchenIssueReturn,
		bool update,
		bool recover,
		KitchenIssueReturnOverviewModel previousKitchenIssueReturn = null,
		List<KitchenIssueReturnItemOverviewModel> previousKitchenIssueReturnDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentKitchenIssueReturn = await CommonData.LoadTableDataById<KitchenIssueReturnOverviewModel>(InventoryNames.KitchenIssueReturnOverview, kitchenIssueReturn.Id, sqlDataAccessTransaction);
			var currentKitchenIssueReturnDetails = await CommonData.LoadTableDataByMasterId<KitchenIssueReturnItemOverviewModel>(InventoryNames.KitchenIssueReturnItemOverview, kitchenIssueReturn.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousKitchenIssueReturn, currentKitchenIssueReturn);
			var detailsDiff = AuditTrailData.GetDifference(previousKitchenIssueReturnDetails, currentKitchenIssueReturnDetails, typeof(KitchenIssueReturnOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = InventoryNames.KitchenIssueReturn,
			RecordNo = kitchenIssueReturn.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? kitchenIssueReturn.LastModifiedBy.Value : kitchenIssueReturn.CreatedBy,
			CreatedFromPlatform = update ? kitchenIssueReturn.LastModifiedFromPlatform : kitchenIssueReturn.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion
}

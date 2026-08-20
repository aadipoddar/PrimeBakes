using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Common;
using PrimeBakes.Data.Inventory.Stock;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Exports.Inventory.Kitchen;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.Kitchen;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Inventory.Stock;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Inventory.Kitchen.KitchenProduction;

public static class KitchenProductionReturnData
{

	private static async Task<int> InsertKitchenProductionReturn(KitchenProductionReturnModel kitchenProductionReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertKitchenProductionReturn, kitchenProductionReturn, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Kitchen Production Return.");

	private static async Task<int> InsertKitchenProductionReturnDetail(KitchenProductionReturnDetailModel kitchenProductionReturnDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertKitchenProductionReturnDetail, kitchenProductionReturnDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Kitchen Production Return Detail.");

	public static async Task<KitchenProductionReturnInvoiceBundle> LoadInvoiceBundle(int transactionId)
	{
		var transaction = await CommonData.LoadTableDataById<KitchenProductionReturnOverviewModel>(InventoryNames.KitchenProductionReturnOverview, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionReturnItemOverviewModel>(InventoryNames.KitchenProductionReturnItemOverview, transaction.Id);
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
	public static async Task DeleteTransaction(KitchenProductionReturnModel kitchenProductionReturn, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(kitchenProductionReturn, transaction));
			await KitchenProductionReturnNotify.Notify(kitchenProductionReturn.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(kitchenProductionReturn.TransactionDateTime, sqlDataAccessTransaction);

		kitchenProductionReturn.Status = false;
		await InsertKitchenProductionReturn(kitchenProductionReturn, sqlDataAccessTransaction);
		await ProductStockData.DeleteProductStockByTransactionNo(kitchenProductionReturn.TransactionNo, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = InventoryNames.KitchenProductionReturn,
			RecordNo = kitchenProductionReturn.TransactionNo,
			CreatedBy = kitchenProductionReturn.LastModifiedBy.Value,
			CreatedFromPlatform = kitchenProductionReturn.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}

	public static async Task RecoverTransaction(KitchenProductionReturnModel kitchenProductionReturn)
	{
		kitchenProductionReturn.Status = true;
		var kitchenProductionReturnDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionReturnDetailModel>(InventoryNames.KitchenProductionReturnDetail, kitchenProductionReturn.Id);
		await SaveTransaction(kitchenProductionReturn, kitchenProductionReturnDetails, true);

		await KitchenProductionReturnNotify.Notify(kitchenProductionReturn.Id, NotifyType.Recovered);
	}
	#endregion

	#region Save
	private static async Task<KitchenProductionReturnModel> ValidateTransaction(KitchenProductionReturnModel kitchenProductionReturn, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		kitchenProductionReturn.Remarks = string.IsNullOrWhiteSpace(kitchenProductionReturn.Remarks) ? null : kitchenProductionReturn.Remarks.Trim();

		if (kitchenProductionReturn.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (kitchenProductionReturn.KitchenId <= 0)
			throw new InvalidOperationException("Please select a kitchen for the transaction.");

		if (kitchenProductionReturn.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (kitchenProductionReturn.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (kitchenProductionReturn.TotalAmount < 0)
			throw new InvalidOperationException("The total amount of the transaction cannot be negative.");

		if (!update)
			kitchenProductionReturn.TransactionNo = await GenerateCodes.GenerateKitchenProductionReturnTransactionNo(kitchenProductionReturn, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(kitchenProductionReturn.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingKitchenProductionReturn = await CommonData.LoadTableDataById<KitchenProductionReturnModel>(InventoryNames.KitchenProductionReturn, kitchenProductionReturn.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingKitchenProductionReturn.TransactionDateTime, sqlDataAccessTransaction);

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, kitchenProductionReturn.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin || user.LocationId != 1)
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			kitchenProductionReturn.TransactionNo = existingKitchenProductionReturn.TransactionNo;
		}

		return kitchenProductionReturn;
	}

	private static void ValidateItemDetails(KitchenProductionReturnModel kitchenProductionReturn, List<KitchenProductionReturnDetailModel> kitchenProductionReturnDetails)
	{
		if (kitchenProductionReturnDetails is null || kitchenProductionReturnDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (kitchenProductionReturnDetails.Count != kitchenProductionReturn.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (kitchenProductionReturnDetails.Sum(ed => ed.Quantity) != kitchenProductionReturn.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		foreach (var item in kitchenProductionReturnDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
	}

	public static async Task<int> SaveTransaction(
		KitchenProductionReturnModel kitchenProductionReturn,
		List<KitchenProductionReturnDetailModel> kitchenProductionReturnDetails,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = kitchenProductionReturn.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? KitchenProductionReturnInvoiceExport.ExportInvoice(await LoadInvoiceBundle(kitchenProductionReturn.Id), InvoiceExportType.PDF) : null;

			kitchenProductionReturn.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(kitchenProductionReturn, kitchenProductionReturnDetails, recover, transaction));

			if (!recover)
				await KitchenProductionReturnNotify.Notify(kitchenProductionReturn.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return kitchenProductionReturn.Id;
		}

		kitchenProductionReturn = await ValidateTransaction(kitchenProductionReturn, update, sqlDataAccessTransaction);
		ValidateItemDetails(kitchenProductionReturn, kitchenProductionReturnDetails);

		var previousKitchenProductionReturn = update && !recover ? await CommonData.LoadTableDataById<KitchenProductionReturnOverviewModel>(InventoryNames.KitchenProductionReturnOverview, kitchenProductionReturn.Id, sqlDataAccessTransaction) : new();
		var previousKitchenProductionReturnDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<KitchenProductionReturnItemOverviewModel>(InventoryNames.KitchenProductionReturnItemOverview, kitchenProductionReturn.Id, sqlDataAccessTransaction) : [];

		kitchenProductionReturn.Id = await InsertKitchenProductionReturn(kitchenProductionReturn, sqlDataAccessTransaction);
		await SaveTransactionDetail(kitchenProductionReturn, kitchenProductionReturnDetails, update, sqlDataAccessTransaction);
		await SaveProductStock(kitchenProductionReturn, kitchenProductionReturnDetails, sqlDataAccessTransaction);
		await SaveAuditTrail(kitchenProductionReturn, update, recover, previousKitchenProductionReturn, previousKitchenProductionReturnDetails, sqlDataAccessTransaction);

		return kitchenProductionReturn.Id;
	}

	private static async Task SaveTransactionDetail(KitchenProductionReturnModel kitchenProductionReturn, List<KitchenProductionReturnDetailModel> kitchenProductionReturnDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingKitchenProductionReturnDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionReturnDetailModel>(InventoryNames.KitchenProductionReturnDetail, kitchenProductionReturn.Id, sqlDataAccessTransaction);
			foreach (var item in existingKitchenProductionReturnDetails)
			{
				item.Status = false;
				await InsertKitchenProductionReturnDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in kitchenProductionReturnDetails)
		{
			item.MasterId = kitchenProductionReturn.Id;
			await InsertKitchenProductionReturnDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveProductStock(KitchenProductionReturnModel kitchenProductionReturn, List<KitchenProductionReturnDetailModel> kitchenProductionReturnDetails, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		await ProductStockData.DeleteProductStockByTransactionNo(kitchenProductionReturn.TransactionNo, sqlDataAccessTransaction);

		foreach (var item in kitchenProductionReturnDetails)
			await ProductStockData.InsertProductStock(new()
			{
				Id = 0,
				ProductId = item.ProductId,
				Quantity = -item.Quantity,
				NetRate = item.Rate,
				Type = nameof(StockType.KitchenProductionReturn),
				TransactionId = kitchenProductionReturn.Id,
				TransactionNo = kitchenProductionReturn.TransactionNo,
				TransactionDateTime = kitchenProductionReturn.TransactionDateTime,
				LocationId = 1, // Main Location
			}, sqlDataAccessTransaction);
	}

	private static async Task SaveAuditTrail(
		KitchenProductionReturnModel kitchenProductionReturn,
		bool update,
		bool recover,
		KitchenProductionReturnOverviewModel previousKitchenProductionReturn = null,
		List<KitchenProductionReturnItemOverviewModel> previousKitchenProductionReturnDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentKitchenProductionReturn = await CommonData.LoadTableDataById<KitchenProductionReturnOverviewModel>(InventoryNames.KitchenProductionReturnOverview, kitchenProductionReturn.Id, sqlDataAccessTransaction);
			var currentKitchenProductionReturnDetails = await CommonData.LoadTableDataByMasterId<KitchenProductionReturnItemOverviewModel>(InventoryNames.KitchenProductionReturnItemOverview, kitchenProductionReturn.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousKitchenProductionReturn, currentKitchenProductionReturn);
			var detailsDiff = AuditTrailData.GetDifference(previousKitchenProductionReturnDetails, currentKitchenProductionReturnDetails, typeof(KitchenProductionReturnOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = InventoryNames.KitchenProductionReturn,
			RecordNo = kitchenProductionReturn.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? kitchenProductionReturn.LastModifiedBy.Value : kitchenProductionReturn.CreatedBy,
			CreatedFromPlatform = update ? kitchenProductionReturn.LastModifiedFromPlatform : kitchenProductionReturn.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion
}

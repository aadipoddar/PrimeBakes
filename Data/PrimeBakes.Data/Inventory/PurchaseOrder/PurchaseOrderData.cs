using PrimeBakes.Data.Accounts.Masters;
using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Utils.Mail;
using PrimeBakes.Exports.Inventory.PurchaseOrder;
using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Inventory.PurchaseOrder;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Inventory.PurchaseOrder;

public static class PurchaseOrderData
{
	private static async Task<int> InsertPurchaseOrder(PurchaseOrderModel purchaseOrder, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchaseOrder, purchaseOrder, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Purchase Order.");

	private static async Task<int> InsertPurchaseOrderDetail(PurchaseOrderDetailModel purchaseOrderDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(InventoryNames.InsertPurchaseOrderDetail, purchaseOrderDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Purchase Order Detail.");

	public static async Task<List<PurchaseOrderModel>> LoadPurchaseOrderByPartyPending(int PartyId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<PurchaseOrderModel, dynamic>(InventoryNames.LoadPurchaseOrderByPartyPending, new { PartyId }, sqlDataAccessTransaction);

	public static async Task LinkPurchaseOrderToPurchase(int? purchaseOrderId = null, int? purchaseId = null, bool unlink = false, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (purchaseOrderId is null or <= 0)
			return;

		var purchaseOrder = await CommonData.LoadTableDataById<PurchaseOrderModel>(InventoryNames.PurchaseOrder, purchaseOrderId.Value, sqlDataAccessTransaction);
		if (purchaseOrder is null || purchaseOrder.Id <= 0 || !purchaseOrder.Status)
			throw new InvalidOperationException("Purchase order not found or is inactive.");

		if (!unlink && purchaseOrder.PurchaseId is not null && purchaseOrder.PurchaseId != purchaseId)
			throw new InvalidOperationException("Purchase order is already linked to another purchase.");

		purchaseOrder.PurchaseId = unlink ? null : purchaseId;
		await InsertPurchaseOrder(purchaseOrder, sqlDataAccessTransaction);
	}

	public static async Task<PurchaseOrderInvoiceBundle> LoadInvoiceBundle(int transactionId)
	{
		var transaction = await CommonData.LoadTableDataById<PurchaseOrderOverviewModel>(InventoryNames.PurchaseOrderOverview, transactionId) ??
			throw new InvalidOperationException("Transaction not found.");

		var transactionDetails = await CommonData.LoadTableDataByMasterId<PurchaseOrderItemOverviewModel>(InventoryNames.PurchaseOrderItemOverview, transaction.Id);
		transactionDetails = [.. transactionDetails.OrderBy(detail => detail.ItemName)];
		if (transactionDetails is null || transactionDetails.Count == 0)
			throw new InvalidOperationException("No transaction details found for the transaction.");

		var company = await CommonData.LoadTableDataById<CompanyModel>(AccountNames.Company, transaction.CompanyId);
		var party = await CommonData.LoadTableDataById<LedgerModel>(AccountNames.Ledger, transaction.PartyId);
		if (company is null || party is null)
			throw new InvalidOperationException("Company or party information is missing.");

		return new(transaction, transactionDetails, company, party, await CommonData.LoadCurrentDateTime());
	}

	#region Delete
	public static async Task DeleteTransaction(PurchaseOrderModel purchaseOrder, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(purchaseOrder, transaction));
			await PurchaseOrderNotify.Notify(purchaseOrder.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(purchaseOrder.TransactionDateTime, sqlDataAccessTransaction);

		if (purchaseOrder.PurchaseId is not null && purchaseOrder.PurchaseId > 0)
			throw new InvalidOperationException("Cannot delete purchase order as it is already converted to a purchase.");

		purchaseOrder.Status = false;
		await InsertPurchaseOrder(purchaseOrder, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = InventoryNames.PurchaseOrder,
			RecordNo = purchaseOrder.TransactionNo,
			CreatedBy = purchaseOrder.LastModifiedBy.Value,
			CreatedFromPlatform = purchaseOrder.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion

	public static async Task RecoverTransaction(PurchaseOrderModel purchaseOrder)
	{
		purchaseOrder.Status = true;
		var purchaseOrderDetails = await CommonData.LoadTableDataByMasterId<PurchaseOrderDetailModel>(InventoryNames.PurchaseOrderDetail, purchaseOrder.Id);
		await SaveTransaction(purchaseOrder, purchaseOrderDetails, true);

		await PurchaseOrderNotify.Notify(purchaseOrder.Id, NotifyType.Recovered);
	}

	#region Save
	private static async Task<PurchaseOrderModel> ValidateTransaction(PurchaseOrderModel purchaseOrder, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		purchaseOrder.Remarks = string.IsNullOrWhiteSpace(purchaseOrder.Remarks) ? null : purchaseOrder.Remarks.Trim();

		if (purchaseOrder.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (purchaseOrder.PartyId <= 0)
			throw new InvalidOperationException("Please select a party for the transaction.");

		if (purchaseOrder.ExpectedDeliveryDate is not null &&
			purchaseOrder.ExpectedDeliveryDate < DateOnly.FromDateTime(purchaseOrder.TransactionDateTime))
			throw new InvalidOperationException("The expected delivery date cannot be before the transaction date.");

		if (purchaseOrder.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (purchaseOrder.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (!update)
			purchaseOrder.TransactionNo = await GenerateCodes.GeneratePurchaseOrderTransactionNo(purchaseOrder, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(purchaseOrder.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingPurchaseOrder = await CommonData.LoadTableDataById<PurchaseOrderModel>(InventoryNames.PurchaseOrder, purchaseOrder.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The transaction to be updated does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingPurchaseOrder.TransactionDateTime, sqlDataAccessTransaction);

			if (existingPurchaseOrder.PurchaseId is not null && existingPurchaseOrder.PurchaseId > 0)
				throw new InvalidOperationException("Cannot update purchase order as it is already converted to a purchase.");

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, purchaseOrder.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin || user.LocationId != 1)
				throw new InvalidOperationException("Only admin users are allowed to modify transactions.");

			purchaseOrder.TransactionNo = existingPurchaseOrder.TransactionNo;
		}

		return purchaseOrder;
	}

	private static void ValidateItemDetails(PurchaseOrderModel purchaseOrder, List<PurchaseOrderDetailModel> purchaseOrderDetails)
	{
		if (purchaseOrderDetails is null || purchaseOrderDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (purchaseOrderDetails.Count != purchaseOrder.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (purchaseOrderDetails.Any(detail => detail.Quantity <= 0))
			throw new InvalidOperationException("Item quantity must be greater than zero.");

		if (purchaseOrderDetails.Any(detail => string.IsNullOrWhiteSpace(detail.UnitOfMeasurement)))
			throw new InvalidOperationException("Please select a unit of measurement for every item.");

		if (purchaseOrderDetails.Sum(detail => detail.Quantity) != purchaseOrder.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		if (purchaseOrderDetails.Any(detail => !detail.Status))
			throw new InvalidOperationException("Purchase order detail items must be active.");

		foreach (var item in purchaseOrderDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
	}

	public static async Task<int> SaveTransaction(
		PurchaseOrderModel purchaseOrder,
		List<PurchaseOrderDetailModel> purchaseOrderDetails,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = purchaseOrder.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover
				? PurchaseOrderInvoiceExport.ExportInvoice(await LoadInvoiceBundle(purchaseOrder.Id), InvoiceExportType.PDF)
				: null;

			purchaseOrder.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(purchaseOrder, purchaseOrderDetails, recover, transaction));

			if (!recover)
				await PurchaseOrderNotify.Notify(purchaseOrder.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return purchaseOrder.Id;
		}

		purchaseOrder = await ValidateTransaction(purchaseOrder, update, sqlDataAccessTransaction);

		purchaseOrderDetails ??= [];
		ValidateItemDetails(purchaseOrder, purchaseOrderDetails);

		var previousPurchaseOrder = update && !recover ? await CommonData.LoadTableDataById<PurchaseOrderOverviewModel>(InventoryNames.PurchaseOrderOverview, purchaseOrder.Id, sqlDataAccessTransaction) : new();
		var previousPurchaseOrderDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<PurchaseOrderItemOverviewModel>(InventoryNames.PurchaseOrderItemOverview, purchaseOrder.Id, sqlDataAccessTransaction) : [];

		purchaseOrder.Id = await InsertPurchaseOrder(purchaseOrder, sqlDataAccessTransaction);
		await SaveTransactionDetail(purchaseOrder, purchaseOrderDetails, update, sqlDataAccessTransaction);
		await SaveAuditTrail(purchaseOrder, update, recover, previousPurchaseOrder, previousPurchaseOrderDetails, sqlDataAccessTransaction);

		return purchaseOrder.Id;
	}

	private static async Task SaveTransactionDetail(PurchaseOrderModel purchaseOrder, List<PurchaseOrderDetailModel> purchaseOrderDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingPurchaseOrderDetails = await CommonData.LoadTableDataByMasterId<PurchaseOrderDetailModel>(InventoryNames.PurchaseOrderDetail, purchaseOrder.Id, sqlDataAccessTransaction);
			foreach (var item in existingPurchaseOrderDetails)
			{
				item.Status = false;
				await InsertPurchaseOrderDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in purchaseOrderDetails)
		{
			item.MasterId = purchaseOrder.Id;
			await InsertPurchaseOrderDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveAuditTrail(
		PurchaseOrderModel purchaseOrder,
		bool update,
		bool recover,
		PurchaseOrderOverviewModel previousPurchaseOrder = null,
		List<PurchaseOrderItemOverviewModel> previousPurchaseOrderDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentPurchaseOrder = await CommonData.LoadTableDataById<PurchaseOrderOverviewModel>(InventoryNames.PurchaseOrderOverview, purchaseOrder.Id, sqlDataAccessTransaction);
			var currentPurchaseOrderDetails = await CommonData.LoadTableDataByMasterId<PurchaseOrderItemOverviewModel>(InventoryNames.PurchaseOrderItemOverview, purchaseOrder.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousPurchaseOrder, currentPurchaseOrder);
			var detailsDiff = AuditTrailData.GetDifference(previousPurchaseOrderDetails, currentPurchaseOrderDetails, typeof(PurchaseOrderOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = InventoryNames.PurchaseOrder,
			RecordNo = purchaseOrder.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? purchaseOrder.LastModifiedBy.Value : purchaseOrder.CreatedBy,
			CreatedFromPlatform = update ? purchaseOrder.LastModifiedFromPlatform : purchaseOrder.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion
}

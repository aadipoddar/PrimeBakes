using PrimeBakes.Data.Accounts.FinancialAccounting;
using PrimeBakes.Data.Common;
using PrimeBakes.Data.Inventory.Kitchen.KitchenIssue;
using PrimeBakes.Data.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Data.Inventory.Purchase;
using PrimeBakes.Data.Inventory.PurchaseOrder;
using PrimeBakes.Data.Store.Order;
using PrimeBakes.Data.Store.Sale;
using PrimeBakes.Data.Store.StockTransfer;
using PrimeBakes.Models.Accounts.FinancialAccounting;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Inventory.Kitchen.KitchenIssue;
using PrimeBakes.Models.Inventory.Kitchen.KitchenProduction;
using PrimeBakes.Models.Inventory.Purchase;
using PrimeBakes.Models.Inventory.PurchaseOrder;
using PrimeBakes.Models.Operations.OfflineQueue;
using PrimeBakes.Models.Store.Order;
using PrimeBakes.Models.Store.Sale;
using PrimeBakes.Models.Store.StockTransfer;

using System.Text.Json;

namespace PrimeBakes.Data.Operations.OfflineQueue;

public static class OfflineQueueData
{
	private static async Task<int> InsertOfflineQueue(OfflineQueueModel offlineQueue, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(OperationNames.InsertOfflineQueue, offlineQueue, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Offline Queue.");

	internal static async Task<int> DeleteOfflineQueueById(int Id, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(OperationNames.DeleteOfflineQueueById, new { Id }, sqlDataAccessTransaction, true)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Delete Offline Queue.");

	#region Save
	private static void ValidateTransaction(OfflineQueueModel offlineQueue)
	{
		offlineQueue.TableName = offlineQueue.TableName?.Trim();
		offlineQueue.TransactionNo = offlineQueue.TransactionNo?.Trim();

		if (string.IsNullOrWhiteSpace(offlineQueue.TableName) || string.IsNullOrWhiteSpace(offlineQueue.TransactionNo))
			throw new Exception("The transaction could not be queued for syncing.");

		if (string.IsNullOrWhiteSpace(offlineQueue.Payload))
			throw new Exception("The transaction has no data to queue for syncing.");
	}

	internal static async Task<int> SaveTransaction(OfflineQueueModel offlineQueue, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		ValidateTransaction(offlineQueue);
		return offlineQueue.Id = await InsertOfflineQueue(offlineQueue, sqlDataAccessTransaction);
	}

	#endregion

	#region Push
	internal static async Task PushOfflineQueue()
	{
		if (OfflineState.Offline || !OfflineState.LocalDBAvailable)
			return;

		var offlineQueues = (await CommonData.LoadTableData<OfflineQueueModel>(OperationNames.OfflineQueue, null, true))
			.OrderBy(offlineQueue => offlineQueue.Id);

		foreach (var offlineQueue in offlineQueues)
			try
			{
				await PushTransaction(offlineQueue);
			}
			catch { }
	}

	private static async Task PushTransaction(OfflineQueueModel offlineQueue)
	{
		#region Accounts
		if (offlineQueue.TableName == AccountNames.FinancialAccounting)
		{
			var request = JsonSerializer.Deserialize<FinancialAccountingSaveRequest>(offlineQueue.Payload);

			request.Accounting.Id = 0;

			foreach (var financialAccountingLedger in request.Ledgers)
			{
				financialAccountingLedger.Id = 0;
				financialAccountingLedger.MasterId = 0;
			}

			await FinancialAccountingData.SaveTransaction(request.Accounting, request.Ledgers, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);
			return;
		}

		#endregion

		#region Inventory
		if (offlineQueue.TableName == InventoryNames.PurchaseOrder)
		{
			var request = JsonSerializer.Deserialize<PurchaseOrderSaveRequest>(offlineQueue.Payload);

			request.PurchaseOrder.Id = 0;
			request.PurchaseOrder.PurchaseId = null;

			foreach (var purchaseOrderDetail in request.Details)
			{
				purchaseOrderDetail.Id = 0;
				purchaseOrderDetail.MasterId = 0;
			}

			await PurchaseOrderData.SaveTransaction(request.PurchaseOrder, request.Details, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);
			return;
		}

		if (offlineQueue.TableName == InventoryNames.Purchase)
		{
			var request = JsonSerializer.Deserialize<PurchaseSaveRequest>(offlineQueue.Payload);

			request.Purchase.Id = 0;
			request.Purchase.FinancialAccountingId = null;
			request.Purchase.PurchaseOrderId = null;

			foreach (var purchaseDetail in request.Details)
			{
				purchaseDetail.Id = 0;
				purchaseDetail.MasterId = 0;
			}

			await PurchaseData.SaveTransaction(request.Purchase, request.Details, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);
			return;
		}

		if (offlineQueue.TableName == InventoryNames.PurchaseReturn)
		{
			var request = JsonSerializer.Deserialize<PurchaseReturnSaveRequest>(offlineQueue.Payload);

			request.PurchaseReturn.Id = 0;
			request.PurchaseReturn.FinancialAccountingId = null;

			foreach (var purchaseReturnDetail in request.Details)
			{
				purchaseReturnDetail.Id = 0;
				purchaseReturnDetail.MasterId = 0;
			}

			await PurchaseReturnData.SaveTransaction(request.PurchaseReturn, request.Details, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);
			return;
		}

		if (offlineQueue.TableName == InventoryNames.KitchenIssue)
		{
			var request = JsonSerializer.Deserialize<KitchenIssueSaveRequest>(offlineQueue.Payload);

			request.KitchenIssue.Id = 0;

			foreach (var kitchenIssueDetail in request.Details)
			{
				kitchenIssueDetail.Id = 0;
				kitchenIssueDetail.MasterId = 0;
			}

			await KitchenIssueData.SaveTransaction(request.KitchenIssue, request.Details, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);
			return;
		}

		if (offlineQueue.TableName == InventoryNames.KitchenIssueReturn)
		{
			var request = JsonSerializer.Deserialize<KitchenIssueReturnSaveRequest>(offlineQueue.Payload);

			request.KitchenIssueReturn.Id = 0;

			foreach (var kitchenIssueReturnDetail in request.Details)
			{
				kitchenIssueReturnDetail.Id = 0;
				kitchenIssueReturnDetail.MasterId = 0;
			}

			await KitchenIssueReturnData.SaveTransaction(request.KitchenIssueReturn, request.Details, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);
			return;
		}

		if (offlineQueue.TableName == InventoryNames.KitchenProduction)
		{
			var request = JsonSerializer.Deserialize<KitchenProductionSaveRequest>(offlineQueue.Payload);

			request.KitchenProduction.Id = 0;

			foreach (var kitchenProductionDetail in request.Details)
			{
				kitchenProductionDetail.Id = 0;
				kitchenProductionDetail.MasterId = 0;
			}

			await KitchenProductionData.SaveTransaction(request.KitchenProduction, request.Details, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);
			return;
		}

		if (offlineQueue.TableName == InventoryNames.KitchenProductionReturn)
		{
			var request = JsonSerializer.Deserialize<KitchenProductionReturnSaveRequest>(offlineQueue.Payload);

			request.KitchenProductionReturn.Id = 0;

			foreach (var kitchenProductionReturnDetail in request.Details)
			{
				kitchenProductionReturnDetail.Id = 0;
				kitchenProductionReturnDetail.MasterId = 0;
			}

			await KitchenProductionReturnData.SaveTransaction(request.KitchenProductionReturn, request.Details, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);
			return;
		}

		#endregion

		#region Store
		if (offlineQueue.TableName == StoreNames.Order)
		{
			var request = JsonSerializer.Deserialize<OrderSaveRequest>(offlineQueue.Payload);

			request.Order.Id = 0;
			request.Order.SaleId = null;

			foreach (var orderDetail in request.OrderDetails)
			{
				orderDetail.Id = 0;
				orderDetail.MasterId = 0;
			}

			await OrderData.SaveTransaction(request.Order, request.OrderDetails, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);
			return;
		}

		if (offlineQueue.TableName == StoreNames.Sale)
		{
			var request = JsonSerializer.Deserialize<SaleSaveRequest>(offlineQueue.Payload);

			request.Sale.Id = 0;
			request.Sale.FinancialAccountingId = null;
			request.Sale.CustomerId = null;
			request.Sale.OrderId = null;

			if (request.Customer is not null)
				request.Customer.Id = 0;

			foreach (var saleDetail in request.SaleDetails)
			{
				saleDetail.Id = 0;
				saleDetail.MasterId = 0;
			}

			await SaleData.SaveTransaction(request.Sale, request.SaleDetails, request.Customer, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);

			return;
		}

		if (offlineQueue.TableName == StoreNames.SaleReturn)
		{
			var request = JsonSerializer.Deserialize<SaleReturnSaveRequest>(offlineQueue.Payload);

			request.SaleReturn.Id = 0;
			request.SaleReturn.FinancialAccountingId = null;
			request.SaleReturn.CustomerId = null;

			if (request.Customer is not null)
				request.Customer.Id = 0;

			foreach (var saleReturnDetail in request.SaleReturnDetails)
			{
				saleReturnDetail.Id = 0;
				saleReturnDetail.MasterId = 0;
			}

			await SaleReturnData.SaveTransaction(request.SaleReturn, request.SaleReturnDetails, request.Customer, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);

			return;
		}

		if (offlineQueue.TableName == StoreNames.StockTransfer)
		{
			var request = JsonSerializer.Deserialize<StockTransferSaveRequest>(offlineQueue.Payload);

			request.StockTransfer.Id = 0;
			request.StockTransfer.FinancialAccountingId = null;

			foreach (var stockTransferDetail in request.StockTransferDetails)
			{
				stockTransferDetail.Id = 0;
				stockTransferDetail.MasterId = 0;
			}

			await StockTransferData.SaveTransaction(request.StockTransfer, request.StockTransferDetails, request.Recover, request.KeepTransactionNo);
			await DeleteOfflineQueueById(offlineQueue.Id);
			return;
		}

		#endregion

		throw new InvalidOperationException($"Cannot sync an unknown offline transaction of type {offlineQueue.TableName}.");
	}
	#endregion
}
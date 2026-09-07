using PrimeBakes.Data.Common;
using PrimeBakes.Data.Store.Order;

using PrimeBakes.Models.Common;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Models.Operations.OfflineQueue;
using PrimeBakes.Models.Store.Order;

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

	private static async Task PushTransaction(OfflineQueueModel offlineQueue)
	{
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
			return;
		}

		throw new InvalidOperationException($"Cannot sync an unknown offline transaction of type {offlineQueue.TableName}.");
	}

	internal static async Task PushOfflineQueue()
	{
		if (OfflineState.Offline)
			return;

		var offlineQueues = (await CommonData.LoadTableData<OfflineQueueModel>(OperationNames.OfflineQueue, null, true))
			.OrderBy(offlineQueue => offlineQueue.Id);

		foreach (var offlineQueue in offlineQueues)
			try
			{
				await PushTransaction(offlineQueue);
				await DeleteOfflineQueueById(offlineQueue.Id);
			}
			catch { }
	}
}
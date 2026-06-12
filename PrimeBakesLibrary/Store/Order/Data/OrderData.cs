using PrimeBakesLibrary.Accounts.Masters.Data;
using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.AuditTrail;
using PrimeBakesLibrary.Operations.User;
using PrimeBakesLibrary.Store.Order.Exports;
using PrimeBakesLibrary.Store.Order.Models;
using PrimeBakesLibrary.Store.Sale.Models;
using PrimeBakesLibrary.Utils.Exports;
using PrimeBakesLibrary.Utils.Mail;

namespace PrimeBakesLibrary.Store.Order.Data;

public static class OrderData
{
	private static async Task<int> InsertOrder(OrderModel order, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertOrder, order, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Order.");

	private static async Task<int> InsertOrderDetail(OrderDetailModel orderDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoreNames.InsertOrderDetail, orderDetail, sqlDataAccessTransaction)).FirstOrDefault()
			is var id and > 0 ? id : throw new InvalidOperationException("Failed to Insert Order Detail.");

	public static async Task<List<OrderModel>> LoadOrderByLocationPending(int LocationId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		await SqlDataAccess.LoadData<OrderModel, dynamic>(StoreNames.LoadOrderByLocationPending, new { LocationId }, sqlDataAccessTransaction);

	public static List<OrderDetailModel> ConvertCartToDetails(List<OrderItemCartModel> cart, int masterId = 0) =>
		[.. cart.Select(item => new OrderDetailModel
		{
			Id = 0,
			MasterId = masterId,
			ProductId = item.ItemId,
			Quantity = item.Quantity,
			Remarks = item.Remarks,
			Status = true
		})];

	public static async Task LinkOrderToSale(SaleModel sale, bool unlink = false, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sale.OrderId is null or <= 0)
			return;

		var order = await CommonData.LoadTableDataById<OrderModel>(StoreNames.Order, sale.OrderId.Value, sqlDataAccessTransaction);
		if (order is null || order.Id <= 0 || !order.Status)
			throw new InvalidOperationException("Order not found or is inactive.");

		if (!unlink && order.SaleId is not null && order.SaleId != sale.Id)
			throw new InvalidOperationException("Order is already linked to another sale.");

		order.SaleId = unlink ? null : sale.Id;
		await InsertOrder(order, sqlDataAccessTransaction);
	}

	#region Delete
	public static async Task DeleteTransaction(OrderModel order, SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		if (sqlDataAccessTransaction is null)
		{
			await SqlDataAccessTransaction.Run(transaction => DeleteTransaction(order, transaction));
			await OrderNotify.Notify(order.Id, NotifyType.Deleted);
			return;
		}

		await FinancialYearData.ValidateFinancialYear(order.TransactionDateTime, sqlDataAccessTransaction);

		if (order.SaleId is not null && order.SaleId > 0)
			throw new InvalidOperationException("Cannot delete order as it is already converted to a sale.");

		order.Status = false;
		await InsertOrder(order, sqlDataAccessTransaction);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Delete.ToString(),
			TableName = StoreNames.Order,
			RecordNo = order.TransactionNo,
			CreatedBy = order.LastModifiedBy.Value,
			CreatedFromPlatform = order.LastModifiedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion

	public static async Task RecoverTransaction(OrderModel order)
	{
		order.Status = true;
		var orderDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(StoreNames.OrderDetail, order.Id);
		await SaveTransaction(order, orderDetails, true);

		await OrderNotify.Notify(order.Id, NotifyType.Recovered);
	}

	#region Save
	private static async Task<OrderModel> ValidateTransaction(OrderModel order, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		order.Remarks = string.IsNullOrWhiteSpace(order.Remarks) ? null : order.Remarks.Trim();

		if (order.CompanyId <= 0)
			throw new InvalidOperationException("Please select a company for the transaction.");

		if (order.LocationId <= 1)
			throw new InvalidOperationException("Please select a location for the transaction.");

		if (order.TotalItems <= 0)
			throw new InvalidOperationException("The total number of items in the transaction must be greater than zero.");

		if (order.TotalQuantity <= 0)
			throw new InvalidOperationException("The total quantity of items in the transaction must be greater than zero.");

		if (!update)
			order.TransactionNo = await GenerateCodes.GenerateOrderTransactionNo(order, sqlDataAccessTransaction);

		await FinancialYearData.ValidateFinancialYear(order.TransactionDateTime, sqlDataAccessTransaction);

		if (update)
		{
			var existingOrder = await CommonData.LoadTableDataById<OrderModel>(StoreNames.Order, order.Id, sqlDataAccessTransaction)
				?? throw new InvalidOperationException("The order transaction does not exist.");

			await FinancialYearData.ValidateFinancialYear(existingOrder.TransactionDateTime, sqlDataAccessTransaction);

			if (existingOrder.SaleId is not null && existingOrder.SaleId > 0)
				throw new InvalidOperationException("Cannot update order as it is already converted to a sale.");

			var user = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, order.LastModifiedBy.Value, sqlDataAccessTransaction);
			if (!user.Admin)
				throw new InvalidOperationException("Only admin users can update an order transaction.");

			order.TransactionNo = existingOrder.TransactionNo;
		}

		return order;
	}

	private static void ValidateItemDetails(OrderModel order, List<OrderDetailModel> orderDetails)
	{
		if (orderDetails is null || orderDetails.Count == 0)
			throw new InvalidOperationException("Please add at least one item detail for the transaction.");

		if (orderDetails.Count != order.TotalItems)
			throw new InvalidOperationException("Total items must be equal to the number of item details.");

		if (orderDetails.Any(ed => ed.Quantity <= 0))
			throw new InvalidOperationException("Item quantity must be greater than zero.");

		if (orderDetails.Sum(ed => ed.Quantity) != order.TotalQuantity)
			throw new InvalidOperationException("Total quantity must be equal to the sum of item quantities.");

		if (orderDetails.Any(ed => !ed.Status))
			throw new InvalidOperationException("Order detail items must be active.");

		foreach (var item in orderDetails)
			item.Remarks = string.IsNullOrWhiteSpace(item.Remarks) ? null : item.Remarks.Trim();
	}

	public static async Task<int> SaveTransaction(
		OrderModel order,
		List<OrderDetailModel> orderDetails,
		bool recover = false,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		bool update = order.Id > 0;

		if (sqlDataAccessTransaction is null)
		{
			(MemoryStream, string)? previousInvoice = update && !recover ? await OrderInvoiceExport.ExportInvoice(order.Id, InvoiceExportType.PDF) : null;

			order.Id = await SqlDataAccessTransaction.Run(transaction => SaveTransaction(order, orderDetails, recover, transaction));

			if (!recover)
				await OrderNotify.Notify(order.Id, update ? NotifyType.Updated : NotifyType.Created, previousInvoice);

			return order.Id;
		}

		order = await ValidateTransaction(order, update, sqlDataAccessTransaction);

		orderDetails ??= ConvertCartToDetails([], order.Id);
		ValidateItemDetails(order, orderDetails);

		var previousOrder = update && !recover ? await CommonData.LoadTableDataById<OrderOverviewModel>(StoreNames.OrderOverview, order.Id, sqlDataAccessTransaction) : new();
		var previousOrderDetails = update && !recover ? await CommonData.LoadTableDataByMasterId<OrderItemOverviewModel>(StoreNames.OrderItemOverview, order.Id, sqlDataAccessTransaction) : [];

		order.Id = await InsertOrder(order, sqlDataAccessTransaction);
		await SaveTransactionDetail(order, orderDetails, update, sqlDataAccessTransaction);
		await SaveAuditTrail(order, update, recover, previousOrder, previousOrderDetails, sqlDataAccessTransaction);

		return order.Id;
	}

	private static async Task SaveTransactionDetail(OrderModel order, List<OrderDetailModel> orderDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
	{
		if (update)
		{
			var existingOrderDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(StoreNames.OrderDetail, order.Id, sqlDataAccessTransaction);
			foreach (var item in existingOrderDetails)
			{
				item.Status = false;
				await InsertOrderDetail(item, sqlDataAccessTransaction);
			}
		}

		foreach (var item in orderDetails)
		{
			item.MasterId = order.Id;
			await InsertOrderDetail(item, sqlDataAccessTransaction);
		}
	}

	private static async Task SaveAuditTrail(
		OrderModel order,
		bool update,
		bool recover,
		OrderOverviewModel previousOrder = null,
		List<OrderItemOverviewModel> previousOrderDetails = null,
		SqlDataAccessTransaction sqlDataAccessTransaction = null)
	{
		string difference = null;

		if (update && !recover)
		{
			var currentOrder = await CommonData.LoadTableDataById<OrderOverviewModel>(StoreNames.OrderOverview, order.Id, sqlDataAccessTransaction);
			var currentOrderDetails = await CommonData.LoadTableDataByMasterId<OrderItemOverviewModel>(StoreNames.OrderItemOverview, order.Id, sqlDataAccessTransaction);

			var headerDiff = AuditTrailData.GetDifference(previousOrder, currentOrder);
			var detailsDiff = AuditTrailData.GetDifference(previousOrderDetails, currentOrderDetails, typeof(OrderOverviewModel));

			difference = AuditTrailData.CombineDifferences(
				(null, headerDiff),
				("Items", detailsDiff));
		}

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = recover ? AuditTrailActionTypes.Recover.ToString() : update ? AuditTrailActionTypes.Update.ToString() : AuditTrailActionTypes.Insert.ToString(),
			TableName = StoreNames.Order,
			RecordNo = order.TransactionNo,
			RecordValue = difference,
			CreatedBy = update ? order.LastModifiedBy.Value : order.CreatedBy,
			CreatedFromPlatform = update ? order.LastModifiedFromPlatform : order.CreatedFromPlatform
		}, sqlDataAccessTransaction);
	}
	#endregion
}

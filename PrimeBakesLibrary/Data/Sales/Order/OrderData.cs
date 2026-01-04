using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Data.Sales.Order;

public static class OrderData
{
	private static async Task<int> InsertOrder(OrderModel order) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertOrder, order)).FirstOrDefault();

	private static async Task<int> InsertOrderDetail(OrderDetailModel orderDetail) =>
		(await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertOrderDetail, orderDetail)).FirstOrDefault();

	public static async Task<List<OrderModel>> LoadOrderByLocationPending(int LocationId) =>
		await SqlDataAccess.LoadData<OrderModel, dynamic>(StoredProcedureNames.LoadOrderByLocationPending, new { LocationId });

	public static async Task UnlinkOrderFromSale(SaleModel sale)
	{
		if (sale.OrderId is null or <= 0)
			return;

		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, sale.OrderId.Value);
		if (order is not null && order.Id > 0)
		{
			order.SaleId = null;
			await InsertOrder(order);
		}
	}

	public static async Task LinkOrderToSale(SaleModel sale)
	{
		if (sale.OrderId is null or <= 0)
			return;

		var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, sale.OrderId.Value);
		if (order is not null && order.Id > 0 && order.Status)
		{
			order.SaleId = sale.Id;
			await InsertOrder(order);
		}
	}

	public static async Task DeleteTransaction(OrderModel order)
	{
		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, order.FinancialYearId);
		if (financialYear is null || financialYear.Locked || !financialYear.Status)
			throw new InvalidOperationException("Cannot delete transaction as the financial year is locked.");

		if (order.SaleId is not null && order.SaleId > 0)
			throw new InvalidOperationException("Cannot delete order as it is already converted to a sale.");

		order.Status = false;
		await InsertOrder(order);
		await SendNotification.OrderNotification(order.Id, NotificationType.Delete);
	}

	public static async Task RecoverTransaction(OrderModel order)
	{
		var transactionDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, order.Id);
		List<OrderItemCartModel> orderItemCarts = [];

		foreach (var item in transactionDetails)
			orderItemCarts.Add(new()
			{
				ItemId = item.ProductId,
				ItemName = "",
				Quantity = item.Quantity,
				Remarks = item.Remarks
			});

		await SaveTransaction(order, orderItemCarts, false);
		await SendNotification.OrderNotification(order.Id, NotificationType.Recover);
	}

	public static async Task<int> SaveTransaction(OrderModel order, List<OrderItemCartModel> orderDetails, bool showNotification = true)
	{
		bool update = order.Id > 0;

		if (update)
		{
			var existingOrder = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, order.Id);
			var updateFinancialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, existingOrder.FinancialYearId);
			if (updateFinancialYear is null || updateFinancialYear.Locked || !updateFinancialYear.Status)
				throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

			if (existingOrder.SaleId is not null && existingOrder.SaleId > 0)
				throw new InvalidOperationException("Cannot update order as it is already converted to a sale.");

			order.TransactionNo = existingOrder.TransactionNo;
		}
		else
			order.TransactionNo = await GenerateCodes.GenerateOrderTransactionNo(order);

		var financialYear = await CommonData.LoadTableDataById<FinancialYearModel>(TableNames.FinancialYear, order.FinancialYearId);
		if (financialYear is null || financialYear.Locked || !financialYear.Status)
			throw new InvalidOperationException("Cannot update transaction as the financial year is locked.");

		order.Id = await InsertOrder(order);
		await SaveTransactionDetail(order, orderDetails, update);

		if (showNotification)
			await SendNotification.OrderNotification(order.Id, update ? NotificationType.Update : NotificationType.Save);

		return order.Id;
	}

	private static async Task SaveTransactionDetail(OrderModel order, List<OrderItemCartModel> orderDetails, bool update)
	{
		if (update)
		{
			var existingOrderDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, order.Id);
			foreach (var item in existingOrderDetails)
			{
				item.Status = false;
				await InsertOrderDetail(item);
			}
		}

		foreach (var item in orderDetails)
			await InsertOrderDetail(new()
			{
				Id = 0,
				MasterId = order.Id,
				ProductId = item.ItemId,
				Quantity = item.Quantity,
				Remarks = item.Remarks,
				Status = true
			});
	}
}
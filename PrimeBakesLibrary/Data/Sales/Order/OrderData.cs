using PrimeBakesLibrary.Data.Accounts.Masters;
using PrimeBakesLibrary.Data.Common;
using PrimeBakesLibrary.Models.Sales.Order;
using PrimeBakesLibrary.Models.Sales.Sale;

namespace PrimeBakesLibrary.Data.Sales.Order;

public static class OrderData
{
    private static async Task<int> InsertOrder(OrderModel order, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertOrder, order, sqlDataAccessTransaction)).FirstOrDefault();

    private static async Task<int> InsertOrderDetail(OrderDetailModel orderDetail, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        (await SqlDataAccess.LoadData<int, dynamic>(StoredProcedureNames.InsertOrderDetail, orderDetail, sqlDataAccessTransaction)).FirstOrDefault();

    public static async Task<List<OrderModel>> LoadOrderByLocationPending(int LocationId, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
        await SqlDataAccess.LoadData<OrderModel, dynamic>(StoredProcedureNames.LoadOrderByLocationPending, new { LocationId }, sqlDataAccessTransaction);

    public static List<OrderDetailModel> ConvertCartToDetails(List<OrderItemCartModel> cart, int orderId) =>
        [.. cart.Select(item => new OrderDetailModel
            {
                Id = 0,
                MasterId = orderId,
                ProductId = item.ItemId,
                Quantity = item.Quantity,
                Remarks = item.Remarks,
                Status = true
            })];

    public static async Task UnlinkOrderFromSale(SaleModel sale, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        if (sale.OrderId is null or <= 0)
            return;

        var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, sale.OrderId.Value, sqlDataAccessTransaction);
        if (order is null || order.Id <= 0)
            throw new InvalidOperationException("Order not found or is inactive.");

        order.SaleId = null;
        await InsertOrder(order, sqlDataAccessTransaction);
    }

    public static async Task LinkOrderToSale(SaleModel sale, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        if (sale.OrderId is null or <= 0)
            return;

        var order = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, sale.OrderId.Value, sqlDataAccessTransaction);
        if (order is null || order.Id <= 0 || !order.Status)
            throw new InvalidOperationException("Order not found or is inactive.");

        if (order.SaleId is not null && order.SaleId != sale.Id)
            throw new InvalidOperationException("Order is already linked to another sale.");

        order.SaleId = sale.Id;
        await InsertOrder(order, sqlDataAccessTransaction);
    }

    public static async Task DeleteTransaction(OrderModel order)
    {
        using SqlDataAccessTransaction sqlDataAccessTransaction = new();

        try
        {
            sqlDataAccessTransaction.StartTransaction();

            await FinancialYearData.ValidateFinancialYear(order.TransactionDateTime, sqlDataAccessTransaction);

            if (order.SaleId is not null && order.SaleId > 0)
                throw new InvalidOperationException("Cannot delete order as it is already converted to a sale.");

            order.Status = false;
            await InsertOrder(order, sqlDataAccessTransaction);
            await SendNotification.OrderNotification(order.Id, NotificationType.Delete);

            sqlDataAccessTransaction.CommitTransaction();
        }
        catch
        {
            sqlDataAccessTransaction.RollbackTransaction();
            throw;
        }
    }

    public static async Task RecoverTransaction(OrderModel order)
    {
        order.Status = true;
        var transactionDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, order.Id);

        await SaveTransaction(order, null, transactionDetails, false);
        await SendNotification.OrderNotification(order.Id, NotificationType.Recover);
    }

    public static async Task<int> SaveTransaction(OrderModel order, List<OrderItemCartModel> cart, List<OrderDetailModel> orderDetails = null, bool showNotification = true, SqlDataAccessTransaction sqlDataAccessTransaction = null)
    {
        bool update = order.Id > 0;

        if (sqlDataAccessTransaction is null)
        {
            using SqlDataAccessTransaction newSqlDataAccessTransaction = new();

            try
            {
                newSqlDataAccessTransaction.StartTransaction();
                order.Id = await SaveTransaction(order, cart, orderDetails, showNotification, newSqlDataAccessTransaction);
                newSqlDataAccessTransaction.CommitTransaction();
            }
            catch
            {
                newSqlDataAccessTransaction.RollbackTransaction();
                throw;
            }

            if (showNotification)
                await SendNotification.OrderNotification(order.Id, update ? NotificationType.Update : NotificationType.Save);

            return order.Id;
        }

        if (update)
        {
            var existingOrder = await CommonData.LoadTableDataById<OrderModel>(TableNames.Order, order.Id, sqlDataAccessTransaction);

            if (existingOrder.SaleId is not null && existingOrder.SaleId > 0)
                throw new InvalidOperationException("Cannot update order as it is already converted to a sale.");

            order.TransactionNo = existingOrder.TransactionNo;
        }
        else
            order.TransactionNo = await GenerateCodes.GenerateOrderTransactionNo(order, sqlDataAccessTransaction);

        await FinancialYearData.ValidateFinancialYear(order.TransactionDateTime, sqlDataAccessTransaction);

        order.Id = await InsertOrder(order, sqlDataAccessTransaction);
        orderDetails ??= ConvertCartToDetails(cart, order.Id);
        await SaveTransactionDetail(order, orderDetails, update, sqlDataAccessTransaction);

        return order.Id;
    }

    private static async Task SaveTransactionDetail(OrderModel order, List<OrderDetailModel> orderDetails, bool update, SqlDataAccessTransaction sqlDataAccessTransaction)
    {
        if (orderDetails is null || orderDetails.Count != order.TotalItems || orderDetails.Sum(od => od.Quantity) != order.TotalQuantity)
            throw new InvalidOperationException("Order details do not match the order summary.");

        if (orderDetails.Any(od => !od.Status))
            throw new InvalidOperationException("Order detail items must be active.");

        if (update)
        {
            var existingOrderDetails = await CommonData.LoadTableDataByMasterId<OrderDetailModel>(TableNames.OrderDetail, order.Id, sqlDataAccessTransaction);
            foreach (var item in existingOrderDetails)
            {
                item.Status = false;
                await InsertOrderDetail(item, sqlDataAccessTransaction);
            }
        }

        foreach (var item in orderDetails)
        {
            item.MasterId = order.Id;
            var id = await InsertOrderDetail(item, sqlDataAccessTransaction);

            if (id <= 0)
                throw new InvalidOperationException("Failed to save order detail item.");
        }
    }
}
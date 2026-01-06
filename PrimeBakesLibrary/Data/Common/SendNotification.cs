using System.Text.Json;

using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Sales.Sale;
using PrimeBakesLibrary.Models.Sales.StockTransfer;

namespace PrimeBakesLibrary.Data.Common;

public static class SendNotification
{
    private static async Task SendNotificationToAPI(List<UserModel> users, string title, string text)
    {
        string endpoint = $"{Secrets.NotificationBackendServiceEndpoint}api/notifications/requests";
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("apikey", Secrets.NotificationAPIKey);

        var notificationPayload = new
        {
            Title = title,
            Text = text,
            Action = "action_a",
            Tags = users.Select(u => u.Id.ToString()).ToArray(),
        };

        string jsonPayload = JsonSerializer.Serialize(notificationPayload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(endpoint, content);
    }

    public static async Task SaleNotification(int saleId, NotifyType type)
    {
        var sale = await CommonData.LoadTableDataById<SaleOverviewModel>(ViewNames.SaleOverview, saleId);
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);

        List<UserModel> targetUsers = [];

        // For Save (new sale creation)
        if (type == NotifyType.Created)
            // Only notify sales and admins of the outlet where the sale was made
            targetUsers = [.. users.Where(u => (u.Admin || u.Sales) && u.LocationId == sale.LocationId)];

        // For Delete, Recover, or Update operations
        else
        {
            // Check if party has a location (is a party outlet)
            if (sale.PartyId != null && sale.PartyId > 0)
            {
                var party = await CommonData.LoadTableDataById<LedgerModel>(TableNames.Ledger, sale.PartyId.Value);

                // If party has a valid location
                if (party.LocationId != null && party.LocationId > 0)
                    // Notify sales and admins of:
                    // 1. The party outlet (where sale was made to)
                    // 2. The main outlet (LocationId = 1)
                    // 3. The outlet where the sale originated from (sale.LocationId)
                    targetUsers = [.. users.Where(u =>
                        (u.Admin || u.Sales) && (
                            u.LocationId == party.LocationId ||     // Party outlet
                            u.LocationId == 1 ||                    // Main outlet
                            u.LocationId == sale.LocationId         // Originating outlet
                        ))];

                else
                    // If party doesn't have a location, notify sales and admins of the originating outlet and main outlet
                    targetUsers = [.. users.Where(u =>
                        (u.Admin || u.Sales) && (
                            u.LocationId == sale.LocationId ||      // Originating outlet
                            u.LocationId == 1                       // Main outlet
                        ))];
            }

            else
                // If no party, notify sales and admins of the originating outlet and main outlet
                targetUsers = [.. users.Where(u =>
                    (u.Admin || u.Sales) && (
                        u.LocationId == sale.LocationId ||          // Originating outlet
                        u.LocationId == 1                           // Main outlet
                    ))];
        }

        // Send notification if there are target users
        if (targetUsers.Count != 0)
        {
            var title = $"Sale {(type == NotifyType.Updated ? "Updated" : type == NotifyType.Deleted ? "Deleted" : type == NotifyType.Recovered ? "Recovered" : "Placed")} | Sale No: {sale.TransactionNo}";
            var text = $"Sale No: {sale.TransactionNo} | Party: {sale.PartyName} | Total Items: {sale.TotalItems} | Total Qty: {sale.TotalQuantity} | Total Amount: {sale.TotalAmount.FormatIndianCurrency()} | User: {sale.CreatedByName} | Date: {sale.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {sale.Remarks}";

            await SendNotificationToAPI(targetUsers, title, text);
        }
    }

    public static async Task SaleReturnNotification(int saleReturnId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Sales && u.LocationId == 1)];

        var saleReturn = await CommonData.LoadTableDataById<SaleReturnOverviewModel>(ViewNames.SaleReturnOverview, saleReturnId);
        var title = $"Sale Return {(type == NotifyType.Updated ? "Updated" : type == NotifyType.Deleted ? "Deleted" : type == NotifyType.Recovered ? "Recovered" : "Placed")} from {saleReturn.PartyName}";
        var text = $"Sale Return No: {saleReturn.TransactionNo} | Party: {saleReturn.PartyName} | Total Items: {saleReturn.TotalItems} | Total Qty: {saleReturn.TotalQuantity} | Total Amount: {saleReturn.TotalAmount.FormatIndianCurrency()} | User: {saleReturn.CreatedByName} | Date: {saleReturn.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {saleReturn.Remarks}";

        await SendNotificationToAPI(users, title, text);
    }

    public static async Task StockTransferNotification(int stockTransferId, NotifyType type)
    {
        var stockTransfer = await CommonData.LoadTableDataById<StockTransferOverviewModel>(ViewNames.StockTransferOverview, stockTransferId);

        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(
            u => u.Admin && u.LocationId == 1 ||
                    u.Sales && u.LocationId == 1 ||
                    u.Admin && u.LocationId == stockTransfer.LocationId ||
                    u.Sales && u.LocationId == stockTransfer.LocationId ||
                    u.Admin && u.LocationId == stockTransfer.ToLocationId ||
                    u.Sales && u.LocationId == stockTransfer.ToLocationId)];

        var title = $"Stock Transfer {(type == NotifyType.Updated ? "Updated" : type == NotifyType.Deleted ? "Deleted" : type == NotifyType.Recovered ? "Recovered" : "Placed")} from {stockTransfer.LocationName} to {stockTransfer.ToLocationName}";
        var text = $"Stock Transfer No: {stockTransfer.TransactionNo} | From: {stockTransfer.LocationName} | To: {stockTransfer.ToLocationName} | Total Items: {stockTransfer.TotalItems} | Total Qty: {stockTransfer.TotalQuantity} | User: {stockTransfer.CreatedByName} | Date: {stockTransfer.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {stockTransfer.Remarks}";

        await SendNotificationToAPI(users, title, text);
    }
}
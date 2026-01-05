using System.Text.Json;

using PrimeBakesLibrary.Models.Accounts.FinancialAccounting;
using PrimeBakesLibrary.Models.Accounts.Masters;
using PrimeBakesLibrary.Models.Common;
using PrimeBakesLibrary.Models.Inventory.Kitchen;
using PrimeBakesLibrary.Models.Inventory.Purchase;
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

    public static async Task FinancialAccountingNotification(int accountingId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Accounts && u.LocationId == 1)];

        var accounting = await CommonData.LoadTableDataById<AccountingOverviewModel>(ViewNames.AccountingOverview, accountingId);
        var title = $"Financial Accounting {(type == NotifyType.Updated ? "Updated" : type == NotifyType.Deleted ? "Deleted" : type == NotifyType.Recovered ? "Recovered" : "Placed")} | Accounting No: {accounting.TransactionNo}";
        var text = $"Accounting No: {accounting.TransactionNo} | Ledgers: {accounting.TotalLedgers} | Total Amount: {accounting.TotalAmount.FormatIndianCurrency()} | User: {accounting.CreatedByName} | Date: {accounting.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {accounting.Remarks}";

        await SendNotificationToAPI(users, title, text);
    }

    public static async Task PurchaseNotification(int purchaseId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var purchase = await CommonData.LoadTableDataById<PurchaseOverviewModel>(ViewNames.PurchaseOverview, purchaseId);
        var title = $"Purchase {(type == NotifyType.Updated ? "Updated" : type == NotifyType.Deleted ? "Deleted" : type == NotifyType.Recovered ? "Recovered" : "Placed")} from {purchase.PartyName}";
        var text = $"Purchase No: {purchase.TransactionNo} | Vendor: {purchase.PartyName} | Total Items: {purchase.TotalItems} | Total Qty: {purchase.TotalQuantity} | Total Amount: {purchase.TotalAmount.FormatIndianCurrency()} | User: {purchase.CreatedByName} | Date: {purchase.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {purchase.Remarks}";

        await SendNotificationToAPI(users, title, text);
    }

    public static async Task PurchaseReturnNotification(int purchaseReturnId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var purchaseReturn = await CommonData.LoadTableDataById<PurchaseReturnOverviewModel>(ViewNames.PurchaseReturnOverview, purchaseReturnId);
        var title = $"Purchase Return {(type == NotifyType.Updated ? "Updated" : type == NotifyType.Deleted ? "Deleted" : type == NotifyType.Recovered ? "Recovered" : "Placed")} from {purchaseReturn.PartyName}";
        var text = $"Purchase Return No: {purchaseReturn.TransactionNo} | Vendor: {purchaseReturn.PartyName} | Total Items: {purchaseReturn.TotalItems} | Total Qty: {purchaseReturn.TotalQuantity} | Total Amount: {purchaseReturn.TotalAmount.FormatIndianCurrency()} | User: {purchaseReturn.CreatedByName} | Date: {purchaseReturn.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {purchaseReturn.Remarks}";

        await SendNotificationToAPI(users, title, text);
    }

    public static async Task KitchenIssueNotification(int kitchenIssueId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var kitchenIssue = await CommonData.LoadTableDataById<KitchenIssueOverviewModel>(ViewNames.KitchenIssueOverview, kitchenIssueId);
        var title = $"Kitchen Issue {(type == NotifyType.Updated ? "Updated" : type == NotifyType.Deleted ? "Deleted" : type == NotifyType.Recovered ? "Recovered" : "Placed")} from {kitchenIssue.KitchenName}";
        var text = $"Kitchen Issue No: {kitchenIssue.TransactionNo} | Total Items: {kitchenIssue.TotalItems} | Total Qty: {kitchenIssue.TotalQuantity} | Total Amount: {kitchenIssue.TotalAmount.FormatIndianCurrency()} | Kitchen: {kitchenIssue.KitchenName} | User: {kitchenIssue.CreatedByName} | Date: {kitchenIssue.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {kitchenIssue.Remarks}";

        await SendNotificationToAPI(users, title, text);
    }

    public static async Task KitchenProductionNotification(int kitchenProductionId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var kitchenProduction = await CommonData.LoadTableDataById<KitchenProductionOverviewModel>(ViewNames.KitchenProductionOverview, kitchenProductionId);
        var title = $"Kitchen Production {(type == NotifyType.Updated ? "Updated" : type == NotifyType.Deleted ? "Deleted" : type == NotifyType.Recovered ? "Recovered" : "Placed")} from {kitchenProduction.KitchenName}";
        var text = $"Kitchen Production No: {kitchenProduction.TransactionNo} | Total Items: {kitchenProduction.TotalItems} | Total Qty: {kitchenProduction.TotalQuantity} | Total Amount: {kitchenProduction.TotalAmount.FormatIndianCurrency()} | Kitchen: {kitchenProduction.KitchenName} | User: {kitchenProduction.CreatedByName} | Date: {kitchenProduction.TransactionDateTime:dd/MM/yy hh:mm tt} | Remarks: {kitchenProduction.Remarks}";

        await SendNotificationToAPI(users, title, text);
    }

    public static async Task RawMaterialStockAdjustmentNotification(int items, decimal quantity, int userId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var title = $"Raw Material Stock Adjustment {(type == NotifyType.Deleted ? "Deleted" : "Placed")}";
        var text = $"Raw Material Stock Adjustment by User: {users.FirstOrDefault(_ => _.Id == userId)?.Name} | Items: {items} | Quantity: {quantity}";

        await SendNotificationToAPI(users, title, text);
    }

    public static async Task ProductStockAdjustmentNotification(int items, decimal quantity, int userId, int locationId, NotifyType type)
    {
        var users = await CommonData.LoadTableDataByStatus<UserModel>(TableNames.User);
        users = [.. users.Where(u => u.Admin && u.LocationId == 1 || u.Inventory && u.LocationId == 1)];

        var title = $"Finished Goods Stock Adjustment {(type == NotifyType.Deleted ? "Deleted" : "Placed")}";
        var text = $"Finished Goods Stock Adjustment by User: {users.FirstOrDefault(_ => _.Id == userId)?.Name} | Items: {items} | Quantity: {quantity} | Location ID: {locationId}";

        await SendNotificationToAPI(users, title, text);
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
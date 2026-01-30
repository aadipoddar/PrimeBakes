using System.Text.Json;

using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakesLibrary.Exporting.Utils;

internal static class NotificationUtil
{
    private static async Task SendNotificationToAPI(List<UserModel> users, string title, string text)
    {
        if (SqlDataAccess._databaseConnection == Secrets.LocalConnectionString)
            return; // Do not send notifications in local/dev environment

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

    internal class TransactionNotificationData
    {
        public string TransactionType { get; set; } // "Order", "Sale", "Purchase", etc.
        public string TransactionNo { get; set; }
        public NotifyType Action { get; set; }
        public string LocationName { get; set; }
        public Dictionary<string, string> Details { get; set; } // Key-value pairs for notification details
        public string Remarks { get; set; }
    }

    internal static async Task SendTransactionNotification(List<UserModel> users, TransactionNotificationData data)
    {
        // Enhanced notification with emoji and better formatting
        var (actionEmoji, actionText) = data.Action switch
        {
            NotifyType.Updated => ("✏️", "Updated"),
            NotifyType.Deleted => ("🗑️", "Deleted"),
            NotifyType.Recovered => ("♻️", "Recovered"),
            _ => ("✅", "Created")
        };

        var title = $"{actionEmoji} {data.TransactionType} {actionText} | {data.LocationName}";

        // Structured notification body with better formatting
        var detailsText = string.Join("\n", data.Details.Select(d => $"{d.Key}: {d.Value}"));
        var remarksText = string.IsNullOrWhiteSpace(data.Remarks) ? "" : $"\n💬 {data.Remarks}";

        var text = $@"{data.TransactionType} #{data.TransactionNo}

{detailsText}{remarksText}";

        await SendNotificationToAPI(users, title, text);
    }
}

public enum NotifyType
{
    Created,
    Updated,
    Recovered,
    Deleted
}
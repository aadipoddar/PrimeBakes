namespace PrimeBakes.Models.Operations.Notification;

public sealed record SendCustomNotificationRequest(List<int> UserIds, string Title, string Text);

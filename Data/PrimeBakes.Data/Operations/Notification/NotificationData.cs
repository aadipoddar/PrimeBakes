using PrimeBakes.Data.Common;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Utils.Notification;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Data.Operations.Notification;

public static class NotificationData
{
	public static async Task SendCustomNotification(List<int> userIds, string title, string text, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude)
	{
		title = title?.Trim();
		text = text?.Trim();

		if (string.IsNullOrWhiteSpace(title))
			throw new InvalidOperationException("Please enter a title for the notification.");

		if (string.IsNullOrWhiteSpace(text))
			throw new InvalidOperationException("Please enter a message for the notification.");

		if (userIds is null || userIds.Count == 0)
			throw new InvalidOperationException("Please select at least one user to notify.");

		var users = (await CommonData.LoadTableDataByStatus<UserModel>(OperationNames.User, true))
			.Where(u => userIds.Contains(u.Id))
			.ToList();

		if (users.Count == 0)
			throw new InvalidOperationException("None of the selected users are active.");

		await NotificationUtil.SendNotificationToAPI(users, title, text);

		await AuditTrailData.SaveAuditTrail(new()
		{
			Action = AuditTrailActionTypes.Notification.ToString(),
			TableName = OperationRouteNames.SendNotification,
			RecordNo = title,
			RecordValue = $"{text}\n\nSent To: {string.Join(", ", users.Select(u => u.Name))}",
			CreatedBy = userId,
			CreatedFormFactor = formFactor,
			CreatedPlatform = platform,
			CreatedLatitude = latitude,
			CreatedLongitude = longitude
		});
	}
}

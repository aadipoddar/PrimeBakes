using PrimeBakes.Data.Operations.Notification;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Notification;

namespace PrimeBakes.Api.Operations.Notification;

public class NotificationEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(NotificationEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapPost(nameof(NotificationData.SendCustomNotification),
			(SendCustomNotificationRequest request, int userId, string formFactor, string platform, decimal? latitude, decimal? longitude) =>
				NotificationData.SendCustomNotification(request.UserIds, request.Title, request.Text, userId, formFactor, platform, latitude, longitude));
	}
}

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Data;
using PrimeBakes.Data.Operations.AuditTrail;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Operations.User;
using PrimeBakes.Models.Operations.AuditTrail;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Shared.Services;

public class AuthenticationService(IDataStorageService dataStorageService, NavigationManager navigationManager, INotificationService notificationService, IVibrationService vibrationService, IFormFactor formFactor, ILocationService locationService)
{
	public async Task<UserModel> ValidateUser(List<UserRoles> userRoles = null, bool primaryLocationRequirement = false)
	{
		ApiClient.Token = await dataStorageService.SecureGetAsync(StorageFileNames.TokenFileName);
		if (string.IsNullOrWhiteSpace(ApiClient.Token))
			await Logout();

		var userData = await dataStorageService.SecureGetAsync(StorageFileNames.UserDataFileName);
		if (string.IsNullOrWhiteSpace(userData))
			await Logout();

		var user = System.Text.Json.JsonSerializer.Deserialize<UserModel>(userData);
		if (user is null)
			await Logout();

		var serverUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, user.Id);
		if (serverUser is null)
			await Logout();

		user = serverUser;

		if (!user.Status)
			await Logout();

		if (primaryLocationRequirement && user.LocationId != 1)
			await Logout();

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var maxLoginTimeSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.MaxLoginTimeHours);
		var maxLoginTimeHours = int.TryParse(maxLoginTimeSetting?.Value, out var hours) && hours > 0 ? hours : 12;

		if (user.LastLoginTime is null || (currentDateTime - user.LastLoginTime.Value).TotalHours > maxLoginTimeHours)
			await Logout();

		var platformInfo = await GetPlatformInfo();
		user.LastSeen = await UserData.UpdateLastSeen(user, platformInfo.FormFactor, platformInfo.Platform, platformInfo.Latitude, platformInfo.Longitude);
		user.LastSeenFormFactor = platformInfo.FormFactor;
		user.LastSeenPlatform = platformInfo.Platform;
		user.LastSeenLatitude = platformInfo.Latitude;
		user.LastSeenLongitude = platformInfo.Longitude;

		await dataStorageService.SecureSaveAsync(StorageFileNames.UserDataFileName, System.Text.Json.JsonSerializer.Serialize(user));

		if (userRoles is null)
			return user;

		var hasPermission = userRoles.All(role => role switch
		{
			UserRoles.Accounts => user.Accounts,
			UserRoles.Inventory => user.Inventory,
			UserRoles.Store => user.Store,
			UserRoles.Restaurant => user.Restaurant,
			UserRoles.Payroll => user.Payroll,
			UserRoles.Reports => user.Reports,
			UserRoles.Admin => user.Admin,
			_ => false
		});

		if (!hasPermission)
			await Logout();

		return user;
	}

	public async Task Logout()
	{
		var userData = await dataStorageService.SecureGetAsync(StorageFileNames.UserDataFileName);
		var user = string.IsNullOrWhiteSpace(userData) ? null : System.Text.Json.JsonSerializer.Deserialize<UserModel>(userData);

		if (user is not null && !string.IsNullOrWhiteSpace(ApiClient.Token))
		{
			var platformInfo = await GetPlatformInfo();
			await UserData.UpdateLastLoginTime(user, null);
			await AuditTrailData.SaveAuditTrail(new()
			{
				Action = AuditTrailActionTypes.Logout.ToString(),
				TableName = OperationNames.User,
				RecordNo = user.Name,
				CreatedBy = user.Id,
				CreatedFormFactor = platformInfo.FormFactor,
				CreatedPlatform = platformInfo.Platform,
				CreatedLatitude = platformInfo.Latitude,
				CreatedLongitude = platformInfo.Longitude
			});
		}

		ApiClient.Token = null;
		await dataStorageService.SecureRemoveAll();
		await notificationService.DeregisterDevicePushNotification();
		vibrationService.VibrateWithTime(500);
		navigationManager.NavigateTo(OperationNames.Login);
	}

	public async Task<PlatformInfoModel> GetPlatformInfo()
	{
		var location = await locationService.GetLocationAsync();
		return new()
		{
			FormFactor = formFactor.GetFormFactor(),
			Platform = formFactor.GetPlatform(),
			Latitude = location?.Latitude,
			Longitude = location?.Longitude
		};
	}

	public static Func<string, bool> OpenRouteInNewWindow { get; set; }
	public static async Task NavigateToRoute(string route, IFormFactor FormFactor, IJSRuntime JSRuntime, NavigationManager NavigationManager)
	{
		if (FormFactor.GetFormFactor() is "Web" or "Wasm")
			await JSRuntime.InvokeVoidAsync("open", route, "_blank");
		else if (OpenRouteInNewWindow is not null && OpenRouteInNewWindow(route))
			return;
		else
			NavigationManager.NavigateTo(route);
	}

	public static Func<bool> CloseCurrentWindow { get; set; }
	public static async Task CloseWindowOrTab(IFormFactor FormFactor, IJSRuntime JSRuntime)
	{
		if (FormFactor.GetFormFactor() is "Web" or "Wasm")
			await JSRuntime.InvokeVoidAsync("pageCloseGuard.close");
		else
			CloseCurrentWindow?.Invoke();
	}
}

public sealed class PlatformInfoModel
{
	public string FormFactor { get; set; }
	public string Platform { get; set; }
	public decimal? Latitude { get; set; }
	public decimal? Longitude { get; set; }
}
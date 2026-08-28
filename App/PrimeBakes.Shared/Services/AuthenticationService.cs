using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Data;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Data.Operations.User;
using PrimeBakes.Models.Operations.Settings;
using PrimeBakes.Models.Operations.User;

namespace PrimeBakes.Shared.Services;

public static class AuthenticationService
{
	public static async Task<UserModel> ValidateUser(IDataStorageService dataStorageService, NavigationManager navigationManager, INotificationService notificationService, IVibrationService vibrationService, List<UserRoles> userRoles = null, bool primaryLocationRequirement = false)
	{
		ApiClient.Token = await dataStorageService.SecureGetAsync(StorageFileNames.TokenFileName);
		if (string.IsNullOrWhiteSpace(ApiClient.Token))
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		var userData = await dataStorageService.SecureGetAsync(StorageFileNames.UserDataFileName);
		if (string.IsNullOrWhiteSpace(userData))
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		var user = System.Text.Json.JsonSerializer.Deserialize<UserModel>(userData);
		if (user is null)
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		var serverUser = await CommonData.LoadTableDataById<UserModel>(OperationNames.User, user.Id);
		if (serverUser is null)
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		user = serverUser;
		await dataStorageService.SecureSaveAsync(StorageFileNames.UserDataFileName, System.Text.Json.JsonSerializer.Serialize(user));

		if (!user.Status)
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		if (primaryLocationRequirement && user.LocationId != 1)
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		var currentDateTime = await CommonData.LoadCurrentDateTime();
		var maxLoginTimeSetting = await SettingsData.LoadSettingsByKey(SettingsKeys.MaxLoginTimeHours);
		var maxLoginTimeHours = int.TryParse(maxLoginTimeSetting?.Value, out var hours) && hours > 0 ? hours : 12;

		if (user.LastLoginTime is null || (currentDateTime - user.LastLoginTime.Value).TotalHours > maxLoginTimeHours)
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		await UserData.UpdateLastSeen(user, currentDateTime);

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
			await Logout(dataStorageService, navigationManager, notificationService, vibrationService);

		return user;
	}

	public static async Task Logout(IDataStorageService dataStorageService, NavigationManager navigationManager, INotificationService notificationService, IVibrationService vibrationService)
	{
		var userData = await dataStorageService.SecureGetAsync(StorageFileNames.UserDataFileName);
		var user = string.IsNullOrWhiteSpace(userData) ? null : System.Text.Json.JsonSerializer.Deserialize<UserModel>(userData);

		if (user is not null && !string.IsNullOrWhiteSpace(ApiClient.Token))
			await UserData.UpdateLastLoginTime(user, null);

		ApiClient.Token = null;
		await dataStorageService.SecureRemoveAll();
		await notificationService.DeregisterDevicePushNotification();
		vibrationService.VibrateWithTime(500);
		navigationManager.NavigateTo(OperationNames.Login);
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

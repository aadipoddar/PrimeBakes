using PrimeBakes.Models.Accounts.Masters;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Settings;

namespace PrimeBakes.Data.Operations.Settings;

public static class SettingsData
{
	private static readonly string _endpoint = Helper.SanitizeClassName(nameof(SettingsData));

	public static async Task<SettingsModel> LoadSettingsByKey(string Key) =>
		await ApiClient.Get<SettingsModel>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadSettingsByKey)), new { Key });

	public static async Task<int> UpdateSettings(SettingsModel settingsModel) =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(UpdateSettings)), settingsModel);

	public static async Task<int> ResetSettings() =>
		await ApiClient.Post<int>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(ResetSettings)), new { });

	public static async Task<CompanyModel> LoadPrimaryCompany() =>
		await ApiClient.Get<CompanyModel>(Helper.MakeRouteFromEndpointFunction(_endpoint, nameof(LoadPrimaryCompany)));
}

using PrimeBakesLibrary.Common;

namespace PrimeBakesLibrary.Operations.Settings;

public static class SettingsData
{
	public static async Task<SettingsModel> LoadSettingsByKey(string Key, SqlDataAccessTransaction sqlDataAccessTransaction = null) =>
		(await SqlDataAccess.LoadData<SettingsModel, dynamic>(OperationNames.LoadSettingsByKey, new { Key }, sqlDataAccessTransaction)).FirstOrDefault();

	public static async Task<int> UpdateSettings(SettingsModel settingsModel) =>
		(await SqlDataAccess.LoadData<int, dynamic>(OperationNames.UpdateSettings, settingsModel)).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Update Settings.");

	public static async Task<int> ResetSettings() =>
		(await SqlDataAccess.LoadData<int, dynamic>(OperationNames.ResetSettings, new { })).FirstOrDefault()
			is var result and > 0 ? result : throw new InvalidOperationException("Failed to Reset Settings.");
}

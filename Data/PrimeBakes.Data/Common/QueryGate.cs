using PrimeBakes.Data.Operations.Settings;

using PrimeBakes.Models.Operations.Settings;

namespace PrimeBakes.Data.Common;

public static class QueryGate
{
	private const int _defaultSpanDays = 30;
	private const int _defaultMaxLoadPercent = 50;

	private static async Task<bool> IsHeavy(DateTime startDate, DateTime endDate)
	{
		var setting = await SettingsData.LoadSettingsByKey(SettingsKeys.HeavyQuerySpanDays);
		var spanDays = int.TryParse(setting?.Value, out var days) && days > 0 ? days : _defaultSpanDays;

		return (endDate - startDate).TotalDays > spanDays;
	}

	public static async Task EnsureCapacity(DateTime startDate, DateTime endDate)
	{
		if (!await IsHeavy(startDate, endDate))
			return;

		var setting = await SettingsData.LoadSettingsByKey(SettingsKeys.HeavyQueryMaxLoadPercent);
		var maxLoadPercent = int.TryParse(setting?.Value, out var percent) && percent > 0 ? percent : _defaultMaxLoadPercent;

		if (await CommonData.LoadDatabaseLoad() > maxLoadPercent)
			throw new InvalidOperationException(
				"The server is busy right now. Please choose a shorter date range, or try again in a minute.");
	}
}

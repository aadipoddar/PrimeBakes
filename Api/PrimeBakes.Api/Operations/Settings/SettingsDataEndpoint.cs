using PrimeBakes.Library.Operations.Settings;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Settings;

namespace PrimeBakes.Api.Operations.Settings;

public class SettingsDataEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(SettingsDataEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint);

		group.MapGet(nameof(SettingsData.LoadSettingsByKey),
			(string Key) => SettingsData.LoadSettingsByKey(Key));

		group.MapPost(nameof(SettingsData.UpdateSettings),
			(SettingsModel settingsModel) => SettingsData.UpdateSettings(settingsModel));

		group.MapPost(nameof(SettingsData.ResetSettings),
			() => SettingsData.ResetSettings());
	}
}

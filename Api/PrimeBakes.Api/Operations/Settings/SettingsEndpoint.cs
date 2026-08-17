using PrimeBakes.Api.Common;
using PrimeBakes.Data.Operations.Settings;
using PrimeBakes.Models.Common;
using PrimeBakes.Models.Operations.Settings;

namespace PrimeBakes.Api.Operations.Settings;

public class SettingsEndpoint : ICarterModule
{
	public void AddRoutes(IEndpointRouteBuilder app)
	{
		var endpoint = Helper.SanitizeClassName(nameof(SettingsEndpoint));
		var group = app.MapGroup(endpoint).WithTags(endpoint).CacheOutput(ApiCachePolicy.Instance);

		group.MapGet(nameof(SettingsData.LoadSettingsByKey),
			(string Key) => SettingsData.LoadSettingsByKey(Key));

		group.MapPost(nameof(SettingsData.UpdateSettings),
			(SettingsModel settingsModel) => SettingsData.UpdateSettings(settingsModel));

		group.MapPost(nameof(SettingsData.ResetSettings),
			() => SettingsData.ResetSettings());

		group.MapGet(nameof(SettingsData.LoadPrimaryCompany),
			() => SettingsData.LoadPrimaryCompany());
	}
}

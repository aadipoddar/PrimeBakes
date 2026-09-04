using Microsoft.AspNetCore.Components;

using PrimeBakes.Data;
using PrimeBakes.Data.Operations.User;

using Syncfusion.Blazor.Inputs;

namespace PrimeBakes.Shared.Pages;

public partial class LoginPage
{
	[Inject] public NavigationManager NavManager { get; set; }

	private string _passcode = "";
	private bool _isVerifying = false;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		ApiClient.Token = null;
		await DataStorageService.SecureRemoveAll();
	}

	private async Task CheckPasscode(OtpInputEventArgs e)
	{
		_passcode = e.Value?.ToString() ?? string.Empty;
		if (_passcode.Length != 4 || _isVerifying)
			return;

		_isVerifying = true;
		StateHasChanged();

		var platformInfo = await PlatformInfo.GetPlatformInfo();
		var login = await AuthData.Login(int.Parse(_passcode), platformInfo.FormFactor, platformInfo.Platform, platformInfo.Latitude, platformInfo.Longitude);

		if (login is null)
		{
			_isVerifying = false;
			StateHasChanged();
			return;
		}

		ApiClient.Token = login.Token;
		await DataStorageService.SecureSaveAsync(StorageFileNames.TokenFileName, login.Token);
		await DataStorageService.SecureSaveAsync(StorageFileNames.UserDataFileName, System.Text.Json.JsonSerializer.Serialize(login.User));
		VibrationService.VibrateWithTime(500);
		NavManager.NavigateTo(OperationRouteNames.Dashboard);
	}
}

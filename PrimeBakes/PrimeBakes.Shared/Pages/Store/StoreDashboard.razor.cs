using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakes.Shared.Pages.Store;

public partial class StoreDashboard : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;

	private bool _isLoading = true;
	private UserModel _user;

	private string Factor =>
		FormFactor.GetFormFactor();

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store]);

		_hotKeysContext = HotKeys.CreateContext()
			.Add(ModCode.Ctrl, Code.B, NavigateToDashboard, "Back", Exclude.None);

		_isLoading = false;
		StateHasChanged();
	}

	private void NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);

	public async ValueTask DisposeAsync()
	{
		if (_hotKeysContext is not null)
			await _hotKeysContext.DisposeAsync();

		GC.SuppressFinalize(this);
	}
}

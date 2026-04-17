using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakes.Shared.Pages.Restaurant;

public partial class RestaurantDashboard : IAsyncDisposable
{
	private HotKeysContext _hotKeysContext;
	private UserModel _user;
	private bool _isLoading = true;

	private string Factor =>
		FormFactor.GetFormFactor();

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);

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

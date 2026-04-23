using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakes.Shared.Pages.Restaurant;

public partial class RestaurantDashboard
{
	private UserModel _user;
	private bool _isLoading = true;

	private string Factor =>
		FormFactor.GetFormFactor();

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);

		_isLoading = false;
		StateHasChanged();
	}

	private void NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);
}

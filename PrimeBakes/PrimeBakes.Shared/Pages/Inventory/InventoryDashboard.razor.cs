using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakes.Shared.Pages.Inventory;

public partial class InventoryDashboard
{
	private UserModel _user;
	private bool _isLoading = true;

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Inventory], true);

		_isLoading = false;
		StateHasChanged();
	}

	private void NavigateToDashboard() =>
		NavigationManager.NavigateTo(PageRouteNames.Dashboard);
}

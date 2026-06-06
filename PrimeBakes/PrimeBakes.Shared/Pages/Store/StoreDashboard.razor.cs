using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.User;

namespace PrimeBakes.Shared.Pages.Store;

public partial class StoreDashboard
{
	private bool _isLoading = true;
	private UserModel _user;

	private string Factor =>
		FormFactor.GetFormFactor();

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!firstRender)
			return;

		_user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Store]);

		_isLoading = false;
		StateHasChanged();
	}

	private void NavigateToDashboard() =>
		NavigationManager.NavigateTo(StoreRouteNames.Dashboard);
}

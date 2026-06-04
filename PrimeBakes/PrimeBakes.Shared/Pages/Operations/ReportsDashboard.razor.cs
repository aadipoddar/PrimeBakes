using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakes.Shared.Pages.Operations;

public partial class ReportsDashboard
{
    private bool _isLoading = true;
    private UserModel _user;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Reports]);

        _isLoading = false;
        StateHasChanged();
    }

    private void NavigateToDashboard() =>
        NavigationManager.NavigateTo(PageRouteNames.Dashboard);

    private async Task Logout() =>
        await AuthenticationService.Logout(DataStorageService, NavigationManager, NotificationService, VibrationService);
}

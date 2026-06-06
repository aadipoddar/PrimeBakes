using PrimeBakesLibrary.Common;
using PrimeBakesLibrary.Operations.User;

namespace PrimeBakes.Shared.Pages.Accounts;

public partial class AccountingDashboard
{
    private UserModel _user;
    private bool _isLoading = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Accounts], true);

        _isLoading = false;
        StateHasChanged();
    }

    private void NavigateBack() =>
        NavigationManager.NavigateTo(StoreRouteNames.Dashboard);
}

using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakes.Shared.Pages.Accounts;

public partial class AccountingDashboard : IAsyncDisposable
{
    private UserModel _user;
    private HotKeysContext _hotKeysContext;
    private bool _isLoading = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Accounts], true);

        _hotKeysContext = HotKeys.CreateContext()
            .Add(ModCode.Ctrl, Code.B, () => NavigationManager.NavigateTo(PageRouteNames.Dashboard), "Back", Exclude.None);

        _isLoading = false;
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hotKeysContext is not null)
            await _hotKeysContext.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}

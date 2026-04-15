using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.DataAccess;
using PrimeBakesLibrary.Models.Operations;

namespace PrimeBakes.Shared.Components.Page;

public partial class Header
{
    private BluetoothReconnectDialog _btDialog;
    private BluetoothDeviceInfo? _btSaved;
    private bool _isBtReconnecting;
    private UserModel _user;

    private bool BtIsConnected => BluetoothPrinterService.IsConnected;
    private bool ShowBtButton => _btSaved is not null;

    protected override async Task OnInitializedAsync() =>
        _user = await AuthenticationService.ValidateUser(DataStorageService, NavigationManager, NotificationService, VibrationService, [UserRoles.Restaurant]);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await LoadBluetoothStatusAsync();
    }

    private async Task LoadBluetoothStatusAsync()
    {
        var json = await DataStorageService.LocalGetAsync(StorageFileNames.BluetoothPrinterDataFileName);
        _btSaved = string.IsNullOrEmpty(json)
            ? null
            : System.Text.Json.JsonSerializer.Deserialize<BluetoothDeviceInfo>(json);
        StateHasChanged();
    }

    private async Task HandleBtClick() =>
        await _btDialog.ShowAsync();

    private async Task HandleBtReconnect()
    {
        try
        {
            _isBtReconnecting = true;
            StateHasChanged();

            var connected = await BluetoothPrinterService.ConnectAsync(_btSaved?.Address ?? string.Empty);
            var info = new BluetoothDeviceInfo
            {
                Name = connected ? BluetoothPrinterService.ConnectedPrinterName : (_btSaved?.Name ?? string.Empty),
                Address = _btSaved?.Address ?? string.Empty,
                IsPaired = true,
                IsConnected = connected
            };
            await DataStorageService.LocalSaveAsync(StorageFileNames.BluetoothPrinterDataFileName, System.Text.Json.JsonSerializer.Serialize(info));
            _btSaved = info;
            await _btDialog.HideAsync();
        }
        catch
        {
            await _btDialog.HideAsync();
        }
        finally
        {
            _isBtReconnecting = false;
            StateHasChanged();
        }
    }

    private async Task HandleBtDismiss() =>
        await _btDialog.HideAsync();

    private async Task HandleBtDisconnect()
    {
        try
        {
            await BluetoothPrinterService.DisconnectAsync();
            await DataStorageService.LocalRemove(StorageFileNames.BluetoothPrinterDataFileName);
            _btSaved = null;
        }
        catch { }
        finally
        {
            StateHasChanged();
        }
    }

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public RenderFragment? LeftContent { get; set; }

    [Parameter]
    public RenderFragment? RightContent { get; set; }
}

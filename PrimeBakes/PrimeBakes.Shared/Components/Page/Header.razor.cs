using Microsoft.AspNetCore.Components;

using PrimeBakes.Shared.Components.Dialog;

using PrimeBakesLibrary.DataAccess;

namespace PrimeBakes.Shared.Components.Page;

public partial class Header
{
    private BluetoothReconnectDialog _btDialog;
    private BluetoothDeviceInfo? _btSaved;
    private bool _isBtReconnecting;

    private bool BtIsConnected => BluetoothPrinterService.IsConnected;
    private bool ShowBtButton => _btSaved is not null;

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

    /// <summary>
    /// The main title displayed in the header center
    /// </summary>
    [Parameter]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The subtitle displayed below the title
    /// </summary>
    [Parameter]
    public string Subtitle { get; set; } = string.Empty;

    /// <summary>
    /// The user name to display (optional, shown before action buttons)
    /// </summary>
    [Parameter]
    public string? UserName { get; set; }

    /// <summary>
    /// Whether to show the logout button
    /// </summary>
    [Parameter]
    public bool ShowLogout { get; set; } = false;

    /// <summary>
    /// Whether to show the home button
    /// </summary>
    [Parameter]
    public bool ShowHome { get; set; } = false;

    /// <summary>
    /// Whether to show the back button
    /// </summary>
    [Parameter]
    public bool ShowBack { get; set; } = false;

    /// <summary>
    /// Callback when logout button is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnLogoutClick { get; set; }

    /// <summary>
    /// Callback when home button is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnHomeClick { get; set; }

    /// <summary>
    /// Callback when back button is clicked
    /// </summary>
    [Parameter]
    public EventCallback OnBackClick { get; set; }

    /// <summary>
    /// Custom content for the right section of the header (replaces default buttons)
    /// Use this for entry pages that need custom action buttons
    /// </summary>
    [Parameter]
    public RenderFragment? RightContent { get; set; }
}

using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Components.Dialog;

/// <summary>
/// Dialog to prompt the user to reconnect to a previously saved Bluetooth printer.
/// Required on web because the Web Bluetooth API needs a user gesture for requestDevice.
/// </summary>
public partial class BluetoothReconnectDialog
{
    private SfDialog _dialog;
    private bool _isVisible;

    [Parameter]
    public string PrinterName { get; set; } = "Unknown";

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback OnReconnect { get; set; }

    [Parameter]
    public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// Shows the reconnect dialog.
    /// </summary>
    public async Task ShowAsync()
    {
        _isVisible = true;
        StateHasChanged();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Hides the reconnect dialog.
    /// </summary>
    public async Task HideAsync()
    {
        _isVisible = false;
        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task HandleReconnect() => await OnReconnect.InvokeAsync();

    private async Task HandleDismiss()
    {
        _isVisible = false;
        await OnDismiss.InvokeAsync();
    }
}

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

    /// <summary>
    /// When true the dialog shows a "Connected" status view instead of the reconnect prompt.
    /// </summary>
    [Parameter]
    public bool IsConnected { get; set; }

    [Parameter]
    public EventCallback OnReconnect { get; set; }

    [Parameter]
    public EventCallback OnDismiss { get; set; }

    /// <summary>
    /// Called when the user explicitly clicks Disconnect while the printer is connected.
    /// </summary>
    [Parameter]
    public EventCallback OnDisconnect { get; set; }

    private async Task HandleDisconnect()
    {
        _isVisible = false;
        await OnDisconnect.InvokeAsync();
    }

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

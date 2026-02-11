using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Components.Dialog;

public partial class ResetConfirmationDialog
{
    private SfDialog _dialog;
    private bool _isVisible;

    [Parameter]
    public string Message { get; set; } = "Are you sure you want to reset all settings to their default values?";

    [Parameter]
    public string WarningMessage { get; set; } = "This will reset all system configuration to default values.";

    [Parameter]
    public string NoteMessage { get; set; } = "This action cannot be undone. You will need to reconfigure all settings manually.";

    [Parameter]
    public string ConfirmButtonText { get; set; } = "Reset Settings";

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback OnConfirm { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    public async Task ShowAsync()
    {
        _isVisible = true;
        StateHasChanged();
        await Task.CompletedTask;
    }

    public async Task HideAsync()
    {
        _isVisible = false;
        StateHasChanged();
        await Task.CompletedTask;
    }

    private async Task HandleConfirm() => await OnConfirm.InvokeAsync();

    private async Task HandleCancel()
    {
        _isVisible = false;
        await OnCancel.InvokeAsync();
    }
}

using Microsoft.AspNetCore.Components;

namespace PrimeBakes.Shared.Components.Dialog;

public partial class MobileValidationErrorDialog
{
    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter]
    public string ContextLabel { get; set; } = "transaction";

    [Parameter]
    public IReadOnlyList<(string Field, string Message)> Errors { get; set; } = [];

    private async Task Close()
    {
        if (VisibleChanged.HasDelegate)
            await VisibleChanged.InvokeAsync(false);
    }
}
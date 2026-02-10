using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Components.Dialog;

public partial class DocumentUploadDialog
{
    private SfDialog _dialog;

    [Parameter] public string Title { get; set; } = "Upload Original Document";
    [Parameter] public string InfoMessage { get; set; } = "Upload the original document for record keeping and future reference.";
    [Parameter] public string ExistingDocumentUrl { get; set; }
    [Parameter] public bool ShowInterpretButton { get; set; } = false;
    [Parameter] public bool IsVisible { get; set; } = false;
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }
    [Parameter] public SfUploader UploaderReference { get; set; }
    
    [Parameter] public EventCallback<UploadChangeEventArgs> OnFileChange { get; set; }
    [Parameter] public EventCallback<RemovingEventArgs> OnFileRemove { get; set; }
    [Parameter] public EventCallback OnDownloadClick { get; set; }
    [Parameter] public EventCallback OnInterpretClick { get; set; }
    [Parameter] public EventCallback OnRemoveClick { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private async Task HandleDialogClose(object args)
    {
        IsVisible = false;
        await IsVisibleChanged.InvokeAsync(IsVisible);
        if (OnClose.HasDelegate)
            await OnClose.InvokeAsync();
    }

    public async Task ShowAsync()
    {
        IsVisible = true;
        await IsVisibleChanged.InvokeAsync(IsVisible);
        StateHasChanged();
    }

    public async Task HideAsync()
    {
        IsVisible = false;
        await IsVisibleChanged.InvokeAsync(IsVisible);
        StateHasChanged();
    }
}

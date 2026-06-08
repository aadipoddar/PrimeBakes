using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Inputs;
using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Components.Dialog;

public partial class DocumentUploadDialog
{
	private SfDialog _dialog;

	[Parameter] public string Title { get; set; } = "Upload Document";
	[Parameter] public string InfoMessage { get; set; } = "Upload the Document for record keeping and future reference.";
	[Parameter] public string ExistingDocumentUrl { get; set; }
	[Parameter] public bool IsVisible { get; set; } = false;
	[Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }

	[Parameter] public EventCallback<UploadChangeEventArgs> OnFileChange { get; set; }
	[Parameter] public EventCallback<RemovingEventArgs> OnFileRemove { get; set; }
	[Parameter] public EventCallback OnDownloadClick { get; set; }
	[Parameter] public EventCallback OnRemoveClick { get; set; }
	[Parameter] public EventCallback OnClose { get; set; }

	private async Task HandleDialogClose(object args)
	{
		IsVisible = false;
		await IsVisibleChanged.InvokeAsync(IsVisible);
		if (OnClose.HasDelegate)
			await OnClose.InvokeAsync();
	}

	private async Task HandleClose() =>
		await HandleDialogClose(null);

	private async Task HandleDownload()
	{
		if (!string.IsNullOrEmpty(ExistingDocumentUrl) && OnDownloadClick.HasDelegate)
			await OnDownloadClick.InvokeAsync();
	}

	private async Task HandleRemove()
	{
		if (!string.IsNullOrEmpty(ExistingDocumentUrl) && OnRemoveClick.HasDelegate)
			await OnRemoveClick.InvokeAsync();
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

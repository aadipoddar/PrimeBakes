using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Popups;

namespace PrimeBakes.Shared.Components.Dialog;

public partial class ConfirmationDialog
{
	private SfDialog _dialog;
	private bool _isVisible;

	[Parameter] public string Title { get; set; } = "Confirm";
	[Parameter] public string Message { get; set; } = "Are you sure you want to proceed?";
	[Parameter] public string ConfirmText { get; set; } = "";

	[Parameter] public EventCallback OnConfirm { get; set; }
	[Parameter] public EventCallback OnCancel { get; set; }

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

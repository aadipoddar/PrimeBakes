using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Components.Dialog;

public enum ToastType
{
	/// <summary>Success toast (green) - for successful operations</summary>
	Success,
	/// <summary>Error toast (red) - for errors and failures</summary>
	Error,
	/// <summary>Warning toast (amber) - for warnings</summary>
	Warning,
	/// <summary>Info toast (blue) - for informational messages</summary>
	Info
}

public partial class ToastNotification : ComponentBase
{
	private SfToast _sfToast = null!;

	/// <summary>
	/// Event callback that fires after a toast is shown, allowing parent to update UI
	/// </summary>
	[Parameter] public EventCallback OnToastShown { get; set; }

	public async Task ShowAsync(string title, string message, ToastType type, int? timeOut = null)
	{
		await HideAllAsync();

		var cssClass = type switch
		{
			ToastType.Success => "e-toast-success",
			ToastType.Error => "e-toast-danger",
			ToastType.Warning => "e-toast-warning",
			ToastType.Info => "e-toast-info",
			_ => "e-toast-success"
		};

		await _sfToast.ShowAsync(new()
		{
			Title = title,
			Content = message,
			CssClass = cssClass,
			Timeout = timeOut ?? 5000
		});

		StateHasChanged();

		if (OnToastShown.HasDelegate)
			await OnToastShown.InvokeAsync();
	}

	public async Task ShowSuccessAsync(string title, string message) => await ShowAsync(title, message, ToastType.Success);
	public async Task ShowErrorAsync(string title, string message) => await ShowAsync(title, message, ToastType.Error);
	public async Task ShowWarningAsync(string title, string message) => await ShowAsync(title, message, ToastType.Warning);
	public async Task ShowInfoAsync(string title, string message) => await ShowAsync(title, message, ToastType.Info);

	public async Task HideAllAsync() => await _sfToast.HideAsync("All");
}

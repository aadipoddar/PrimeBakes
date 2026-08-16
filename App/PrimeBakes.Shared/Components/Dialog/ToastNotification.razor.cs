using Microsoft.AspNetCore.Components;

using Syncfusion.Blazor.Notifications;

namespace PrimeBakes.Shared.Components.Dialog;

public enum ToastType
{
	Success,
	Error,
	Warning,
	Info
}

public partial class ToastNotification : ComponentBase
{
	private SfToast _sfToast = null!;
	private SfToast _sfInfoToast = null!;

	[Parameter] public EventCallback OnToastShown { get; set; }

	public async Task ShowAsync(string title, string message, ToastType type, int? timeout = null)
	{
		await HideAllInfoAsync();

		var cssClass = type switch
		{
			ToastType.Success => "e-toast-success",
			ToastType.Error => "e-toast-danger",
			ToastType.Warning => "e-toast-warning",
			ToastType.Info => "e-toast-info",
			_ => "e-toast-success"
		};

		if (type == ToastType.Info)
			await _sfInfoToast.ShowAsync(new()
			{
				Title = title,
				Content = message,
				CssClass = cssClass,
				Timeout = timeout ?? 0
			});

		else
			await _sfToast.ShowAsync(new()
			{
				Title = title,
				Content = message,
				CssClass = cssClass,
				Timeout = timeout ?? 5000
			});

		StateHasChanged();

		if (OnToastShown.HasDelegate)
			await OnToastShown.InvokeAsync();
	}

	public async Task HideAllAsync() => await _sfToast.HideAsync("All");
	public async Task HideAllInfoAsync() => await _sfInfoToast?.HideAsync("All");
}
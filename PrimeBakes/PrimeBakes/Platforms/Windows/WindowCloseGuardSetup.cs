using Microsoft.Maui.LifecycleEvents;

using PrimeBakes.Shared.Services;

namespace PrimeBakes.Services;

internal static class WindowCloseGuardSetup
{
	public static MauiAppBuilder UseWindowCloseGuard(this MauiAppBuilder builder)
	{
		builder.ConfigureLifecycleEvents(events =>
			events.AddWindows(windows => windows.OnWindowCreated(HookWindowClose)));
		return builder;
	}

	private static void HookWindowClose(Microsoft.UI.Xaml.Window window)
	{
		var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
		var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
		var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

		var forceClose = false;   // set once the user confirms, so the re-raised close passes through
		var confirming = false;   // guards against re-entry while the dialog is open

		appWindow.Closing += async (sender, e) =>
		{
			if (forceClose || !WindowCloseGuard.BlockClose)
				return;

			// Must be set synchronously, before the first await, for the runtime to honour it.
			e.Cancel = true;

			if (confirming)
				return;
			confirming = true;

			try
			{
				var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
				{
					Title = "Close window?",
					Content = WindowCloseGuard.Message,
					PrimaryButtonText = "Close",
					CloseButtonText = "Stay",
					DefaultButton = Microsoft.UI.Xaml.Controls.ContentDialogButton.Close,
					XamlRoot = window.Content.XamlRoot,
				};

				if (await dialog.ShowAsync() == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
				{
					forceClose = true;
					window.Close();
				}
			}
			catch { }
			finally { confirming = false; }
		};
	}
}

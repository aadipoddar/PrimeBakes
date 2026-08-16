using PrimeBakes.Shared.Services;

namespace PrimeBakes;

public partial class App
{
	partial void ConfigurePlatform()
	{
		AuthenticationService.OpenRouteInNewWindow = route =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				var window = new Window(new MainPage(route)) { Title = "PrimeBakes" };
				Current?.OpenWindow(window);
				MaximizeWindow(window);
			});
			return true;
		};

		AuthenticationService.CloseCurrentWindow = () =>
		{
			MainThread.BeginInvokeOnMainThread(CloseForegroundWindow);
			return true;
		};
	}

	private static void MaximizeWindow(Window window)
	{
		if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window native)
			return;

		var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(WinRT.Interop.WindowNative.GetWindowHandle(native));
		if (Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId).Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
			presenter.Maximize();
	}

	private void CloseForegroundWindow()
	{
		var foreground = GetForegroundWindow();
		foreach (var window in Windows)
		{
			if (window.Handler?.PlatformView is Microsoft.UI.Xaml.Window native
				&& WinRT.Interop.WindowNative.GetWindowHandle(native) == foreground)
			{
				CloseWindow(window);
				return;
			}
		}
	}

	[System.Runtime.InteropServices.DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();
}

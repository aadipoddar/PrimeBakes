// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.UI.Windowing;

using WinRT.Interop;
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace PrimeBakes.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // Maximize the first MAUI window when the app starts.
        if (Microsoft.Maui.Controls.Application.Current?.Windows.Count > 0)
        {
			if (Microsoft.Maui.Controls.Application.Current.Windows[0].Handler?.PlatformView is not WinUIWindow mauiWindow)
				return;

			var hWnd = WindowNative.GetWindowHandle(mauiWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            if (appWindow.Presenter is OverlappedPresenter presenter)
                presenter.Maximize();
        }
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

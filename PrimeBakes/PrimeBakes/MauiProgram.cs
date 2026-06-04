#if DEBUG
using Microsoft.Extensions.Logging;
#endif

using PrimeBakes.Services;
using PrimeBakes.Shared.Services;

using PrimeBakesLibrary.DataAccess;

using Syncfusion.Blazor;
using MudBlazor.Services;

namespace PrimeBakes;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
		Secrets.SetupConfiguration();

		var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .RegisterServices()
            .RegisterViews()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add device-specific services used by the PrimeBakes.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();
        builder.Services.AddSingleton<ISaveAndViewService, SaveAndViewService>();
        builder.Services.AddSingleton<IUpdateService, UpdateService>();
        builder.Services.AddSingleton<IDataStorageService, DataStorageService>();
        builder.Services.AddSingleton<IVibrationService, VibrationService>();
        builder.Services.AddSingleton<ISoundService, SoundService>();
        builder.Services.AddSingleton<IBluetoothPrinterService, BluetoothPrinterService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IThermalPrintDispatcher, ThermalPrintDispatcher>();

#if WINDOWS
        builder.Services.AddSingleton<IDirectPrintService, Platforms.Windows.WindowsDirectPrintService>();
#else
        builder.Services.AddSingleton<IDirectPrintService, NullDirectPrintService>();
#endif

		builder.Services
			.AddSyncfusionBlazor()
			.AddMudServices()
			.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    public static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
    {
#if ANDROID
        builder.Services.AddSingleton<IDeviceInstallationService, Platforms.Android.DeviceInstallationService>();
#endif

        builder.Services.AddSingleton<IPushDemoNotificationActionService, PushDemoNotificationActionService>();
        builder.Services.AddSingleton<INotificationRegistrationService>(new NotificationRegistrationService(Secrets.NotificationBackendServiceEndpoint, Secrets.NotificationAPIKey));

        return builder;
    }

    public static MauiAppBuilder RegisterViews(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<MainPage>();
        return builder;
    }
}

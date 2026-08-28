#if DEBUG
using Microsoft.Extensions.Logging;
#endif

using MudBlazor.Services;

using PrimeBakes.Data;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Services;
using PrimeBakes.Shared.Services;

using Syncfusion.Blazor;
using Syncfusion.Licensing;

using System.Net;

namespace PrimeBakes;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		SyncfusionLicenseProvider.RegisterLicense(CommonSecrets.SyncfusionLicense);
		ApiClient.Init(new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
		{
			BaseAddress = new Uri(CommonSecrets.ApiBaseUrl),
			Timeout = TimeSpan.FromMinutes(10)
		});

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.RegisterServices()
			.RegisterViews()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddSingleton<IFormFactor, FormFactor>();
		builder.Services.AddSingleton<ISaveAndViewService, SaveAndViewService>();
		builder.Services.AddSingleton<IUpdateService, UpdateService>();
		builder.Services.AddSingleton<IDataStorageService, DataStorageService>();
		builder.Services.AddSingleton<IVibrationService, VibrationService>();
		builder.Services.AddSingleton<ISoundService, SoundService>();
		builder.Services.AddSingleton<ILocationService, LocationService>();
		builder.Services.AddSingleton<IBluetoothPrinterService, BluetoothPrinterService>();
		builder.Services.AddScoped<INotificationService, NotificationService>();
		builder.Services.AddScoped<IThermalPrintDispatcher, ThermalPrintDispatcher>();
		builder.Services.AddScoped<PageRefreshState>();
		builder.Services.AddScoped<AuthenticationService>();

#if WINDOWS
        builder.Services.AddSingleton<IDirectPrintService, Platforms.Windows.WindowsDirectPrintService>();
#else
		builder.Services.AddSingleton<IDirectPrintService, NullDirectPrintService>();
#endif

		builder.Services
			.AddSyncfusionBlazor()
			.AddMudServices()
			.AddMauiBlazorWebView();

#if WINDOWS
		builder.UseWindowCloseGuard();
#endif

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
		builder.Services.AddSingleton<INotificationRegistrationService>(new NotificationRegistrationService(CommonSecrets.NotificationBackendServiceEndpoint, CommonSecrets.NotificationAPIKey));

		return builder;
	}

	public static MauiAppBuilder RegisterViews(this MauiAppBuilder builder)
	{
		builder.Services.AddSingleton<MainPage>();
		return builder;
	}
}

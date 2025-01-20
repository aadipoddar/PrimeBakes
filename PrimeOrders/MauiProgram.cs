using System.Reflection;

#if DEBUG
using Microsoft.Extensions.Logging;
#endif


#if ANDROID
using PrimeOrders.Platforms.Android;
#endif

using Syncfusion.Maui.Core.Hosting;

namespace PrimeOrders;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{

#if ANDROID
		var currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

		if (Task.Run(async () => await AadiSoftUpdater.CheckForUpdates("aadipoddar", "PrimeBakes", currentVersion)).Result)
			Task.Run(async () => await AadiSoftUpdater.UpdateApp("aadipoddar", "PrimeBakes", "com.aadisoft.primeorders"));
#endif

		Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(Secrets.SyncfusionLicense);
		var builder = MauiApp.CreateBuilder();
		builder.ConfigureSyncfusionCore();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}

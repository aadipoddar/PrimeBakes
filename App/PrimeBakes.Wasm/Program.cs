using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

using PrimeBakes.Data;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Shared.Services;
using PrimeBakes.Shared.Services.Host;
using PrimeBakes.Shared.Services.Device;
using PrimeBakes.Shared.Services.Notification;
using PrimeBakes.Shared.Services.Printing;
using PrimeBakes.Shared.Services.Storage;
using PrimeBakes.Wasm;
using PrimeBakes.Wasm.Services.Host;
using PrimeBakes.Wasm.Services.Device;
using PrimeBakes.Wasm.Services.Storage;

using Syncfusion.Blazor;
using Syncfusion.Licensing;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

SyncfusionLicenseProvider.RegisterLicense(CommonSecrets.SyncfusionLicense);
ApiClient.Init(new HttpClient
{
	BaseAddress = new Uri(CommonSecrets.ApiBaseUrl),
	Timeout = TimeSpan.FromMinutes(10)
});

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
	.AddSyncfusionBlazor()
	.AddMudServices();

builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<IUpdateService, UpdateService>();
builder.Services.AddSingleton<ILocalDbService, LocalDbService>();
builder.Services.AddSingleton<IVibrationService, VibrationService>();
builder.Services.AddSingleton<INotificationService, BrowserNotificationService>();
builder.Services.AddSingleton<IBluetoothPrinterService, NullBluetoothPrinterService>();
builder.Services.AddSingleton<IDirectPrintService, NullDirectPrintService>();

builder.Services.AddScoped<ISaveAndViewService, SaveAndViewService>();
builder.Services.AddScoped<ISoundService, SoundService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IDataStorageService, DataStorageService>();
builder.Services.AddScoped<IThermalPrintDispatcher, ThermalPrintDispatcher>();
builder.Services.AddScoped<PageRefreshState>();
builder.Services.AddScoped<WindowNavigation>();
builder.Services.AddScoped<PlatformInfoService>();
builder.Services.AddScoped<AuthenticationService>();

await builder.Build().RunAsync();

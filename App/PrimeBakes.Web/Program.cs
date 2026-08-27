using MudBlazor.Services;

using PrimeBakes.Data;
using PrimeBakes.Models.DataAccess;
using PrimeBakes.Shared.Services;
using PrimeBakes.Web.Components;
using PrimeBakes.Web.Services;

using Syncfusion.Blazor;
using Syncfusion.Licensing;

using System.Net;

var builder = WebApplication.CreateBuilder(args);

SyncfusionLicenseProvider.RegisterLicense(CommonSecrets.SyncfusionLicense);
ApiClient.Init(new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All })
{
	BaseAddress = new Uri(CommonSecrets.ApiBaseUrl),
	Timeout = TimeSpan.FromMinutes(10)
});

builder.Services
	.AddSyncfusionBlazor()
	.AddMudServices()
	.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<IUpdateService, UpdateService>();
builder.Services.AddSingleton<IVibrationService, VibrationService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

builder.Services.AddScoped<ISaveAndViewService, SaveAndViewService>();
builder.Services.AddScoped<ISoundService, SoundService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IDataStorageService, DataStorageService>();
builder.Services.AddScoped<IThermalPrintDispatcher, ThermalPrintDispatcher>();
builder.Services.AddSingleton<IBluetoothPrinterService, NullBluetoothPrinterService>();
builder.Services.AddSingleton<IDirectPrintService, NullDirectPrintService>();
builder.Services.AddScoped<PageRefreshState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error", createScopeForErrors: true);
	app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode()
	.AddAdditionalAssemblies(
		typeof(PrimeBakes.Shared._Imports).Assembly);

app.Run();

using Microsoft.Extensions.Configuration;

using System.Reflection;

namespace PrimeBakesLibrary.DataAccess;

public static partial class Secrets
{
	public static string DatabaseName => "PrimeBakes";

	public static string AzureConnectionString = GetSecret(nameof(AzureConnectionString));
	public static string LocalConnectionString = GetSecret(nameof(LocalConnectionString));

	public static string AzureBlobStorageAccountName => "primebakesstore";
	public static string AzureBlobStorageConnectionString = GetSecret(nameof(AzureBlobStorageConnectionString));
	public static string AzureBlobStorageAccountKey = GetSecret(nameof(AzureBlobStorageAccountKey));

	public static string NotificationAPIKey = GetSecret(nameof(NotificationAPIKey));
	public static string NotificationBackendServiceEndpoint = GetSecret(nameof(NotificationBackendServiceEndpoint));

	public static string SyncfusionLicense = GetSecret(nameof(SyncfusionLicense));

	public static string Email => "softaadi@gmail.com";
	public static string EmailPassword = GetSecret(nameof(EmailPassword));

	public static string ToEmail = GetSecret(nameof(ToEmail));
	public static string ToName => "Prime Bakes";

	public static string OnlineFullLogoPath => "https://raw.githubusercontent.com/aadipoddar/PrimeBakes/refs/heads/main/PrimeBakes/PrimeBakes.Web/wwwroot/images/logo_full.png";
	public static string AadiSoftWebsite => "https://aadisoft.vercel.app";
	public static string AppWebsite => "https://primebakes.azurewebsites.net";

	private static string GetSecret(string key) =>
		new ConfigurationBuilder()
			.AddUserSecrets(Assembly.GetExecutingAssembly())
			.AddEnvironmentVariables()
			.Build()
			.GetSection(key).Value;
}
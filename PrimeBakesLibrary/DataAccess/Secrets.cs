namespace PrimeBakesLibrary.DataAccess;

public static class Secrets
{
    public static string AzureConnectionString => Environment.GetEnvironmentVariable(nameof(AzureConnectionString)) ?? "";
    public static string LocalConnectionString => Environment.GetEnvironmentVariable(nameof(LocalConnectionString)) ?? "";

    public static string DatabaseUserId => Environment.GetEnvironmentVariable(nameof(DatabaseUserId)) ?? "";
    public static string DatabasePassword => Environment.GetEnvironmentVariable(nameof(DatabasePassword)) ?? "";
    public static string DatabaseName => "PrimeBakes";

    public static string AzureBlobStorageConnectionString => Environment.GetEnvironmentVariable(nameof(AzureBlobStorageConnectionString)) ?? "";
    public static string AzureBlobStorageAccountName => Environment.GetEnvironmentVariable(nameof(AzureBlobStorageAccountName)) ?? "";
    public static string AzureBlobStorageAccountKey => Environment.GetEnvironmentVariable(nameof(AzureBlobStorageAccountKey)) ?? "";

    public static string SyncfusionLicense => Environment.GetEnvironmentVariable(nameof(SyncfusionLicense)) ?? "";

    public static string NotificationAPIKey => Environment.GetEnvironmentVariable(nameof(NotificationAPIKey)) ?? "";
    public static string NotificationBackendServiceEndpoint => Environment.GetEnvironmentVariable(nameof(NotificationBackendServiceEndpoint)) ?? "";

    public static string Email => "softaadi@gmail.com";
    public static string EmailPassword => Environment.GetEnvironmentVariable(nameof(EmailPassword)) ?? "";

    public static string ToEmail => Environment.GetEnvironmentVariable(nameof(ToEmail)) ?? "";
    public static string ToName => "Prime Bakes";

    public static string OnlineFullLogoPath => "https://raw.githubusercontent.com/aadipoddar/PrimeBakes/refs/heads/main/PrimeBakes/PrimeBakes.Web/wwwroot/images/logo_full.png";
    public static string AadiSoftWebsite => "https://aadisoft.vercel.app";
    public static string AppWebsite => "https://primebakes.azurewebsites.net";
}

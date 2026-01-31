namespace PrimeBakesLibrary.DataAccess;

public static partial class Secrets
{
    public static string DatabaseName => "PrimeBakes";

    public static string AzureConnectionString = "";
    public static string LocalConnectionString => "Data Source=AADILAPIKIIT;Initial Catalog=PrimeBakes;Integrated Security=True;Connect Timeout=300;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

    public static string AzureBlobStorageAccountName => "primebakesstore";
    public static string AzureBlobStorageConnectionString = "";
    public static string AzureBlobStorageAccountKey = "";

    public static string NotificationAPIKey = "";
    public static string NotificationBackendServiceEndpoint = "";

    public static string SyncfusionLicense = "";

    public static string Email => "softaadi@gmail.com";
    public static string EmailPassword = "";

    public static string ToEmail = "";
    public static string ToName => "Prime Bakes";

    public static string OnlineFullLogoPath => "https://raw.githubusercontent.com/aadipoddar/PrimeBakes/refs/heads/main/PrimeBakes/PrimeBakes.Web/wwwroot/images/logo_full.png";
    public static string AadiSoftWebsite => "https://aadisoft.vercel.app";
    public static string AppWebsite => "https://primebakes.azurewebsites.net";
}

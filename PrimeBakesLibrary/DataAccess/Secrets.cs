namespace PrimeBakesLibrary.DataAccess;

public static partial class Secrets
{
	public static readonly string DatabaseName = "PrimeBakes";

	public static readonly string AzureConnectionString;
	public static readonly string AzureTestingConnectionString;
	public static readonly string LocalConnectionString = "Data Source=AADILAPIKIIT;Initial Catalog=PrimeBakes;Integrated Security=True;Connect Timeout=300;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

	public static readonly string AzureBlobStorageAccountName = "primebakesstore";
	public static readonly string AzureBlobStorageConnectionString;
	public static readonly string AzureBlobStorageAccountKey;

	public static readonly string NotificationAPIKey;
	public static readonly string NotificationBackendServiceEndpoint;

	public static readonly string SyncfusionLicense;

	public static readonly string Email = "softaadi@gmail.com";
	public static readonly string EmailPassword;

	public static readonly string ToName = "Prime Bakes";

	public static readonly string OnlineFullLogoPath = "https://raw.githubusercontent.com/aadipoddar/PrimeBakes/refs/heads/main/PrimeBakes/PrimeBakes.Web/wwwroot/images/logo_full.png";
	public static readonly string AadiSoftWebsite = "https://aadisoft.vercel.app";
	public static readonly string AppWebsite = "https://primebakes.azurewebsites.net";
}
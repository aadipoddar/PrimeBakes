namespace PrimeBakes.Data.DataAccess;

public static partial class Secrets
{
	public static readonly string AzureConnectionString;
	public static readonly string AzureTestingConnectionString;
	public static readonly string LocalConnectionString = "Data Source=AADILAPIKIIT;Initial Catalog=PrimeBakes;Integrated Security=True;Connect Timeout=300;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";
	public static readonly string LocalClientConnectionString = "Data Source=.\\AadiSoft;Initial Catalog=PrimeBakesClient;Integrated Security=True;Connect Timeout=300;Encrypt=True;Trust Server Certificate=True;Application Intent=ReadWrite;Multi Subnet Failover=False";

	public static readonly string AzureBlobStorageAccountName = "primebakesstore";
	public static readonly string AzureBlobStorageConnectionString;
	public static readonly string AzureBlobStorageAccountKey;

	public static readonly string JwtKey;
	public static readonly string GoogleMapsApiKey;
	public static readonly string WebPushPrivateKey;

	public static readonly string Email = "softaadi@gmail.com";
	public static readonly string EmailPassword;

	public static readonly string ToName = "Prime Bakes";
}
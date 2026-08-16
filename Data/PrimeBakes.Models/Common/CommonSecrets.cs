namespace PrimeBakes.Models.Common;

public static partial class CommonSecrets
{
	public static readonly ConnectionType DatabaseConnection = ConnectionType.Local;

	public static readonly string ApiBaseUrl = DatabaseConnection switch
	{
		ConnectionType.Local => "https://localhost:7038/",
		ConnectionType.Azure => "https://primebakes-api.azurewebsites.net/",
		ConnectionType.AzureTesting => "https://primebakes-api-testing.azurewebsites.net/",
		_ => throw new NotImplementedException("The specified API connection type is not implemented.")
	};

	public static readonly string SyncfusionLicense;

	public static readonly string DatabaseName = "PrimeBakes";

	public static readonly string OnlineFullLogoPath = "https://raw.githubusercontent.com/aadipoddar/PrimeBakes/refs/heads/main/App/PrimeBakes.Web/wwwroot/images/logo_full.png";
	public static readonly string AadiSoftWebsite = "https://aadisoft.vercel.app";
	public static readonly string AppWebsite = "https://primebakes.azurewebsites.net";

	public static readonly List<int> SuperAdminIds = [1, 46];
}

public enum ConnectionType
{
	Local,
	Azure,
	AzureTesting
}

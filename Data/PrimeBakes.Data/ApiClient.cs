namespace PrimeBakes.Data;

public static class ApiClient
{
	public static void Init(HttpClient http) => SqlDataAccess.SetupConfiguration();

	public static string Token { get; set; }
}

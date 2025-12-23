using PrimeBakesLibrary.DataAccess;

namespace PrimeBakes;

public static partial class Config
{
    public static string ApiKey => Secrets.NotificationAPIKey;
    public static string BackendServiceEndpoint => Secrets.NotificationBackendServiceEndpoint;
}
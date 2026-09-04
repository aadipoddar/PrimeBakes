namespace PrimeBakes.Shared.Services.Host;

public interface ILocalDbService
{
	Task<bool> LocalDBAvailable();

	Task SyncDataBackground();

	Task InstallSqlServer();

	Task SetupDatabase();

	Task UninstallSqlServer();
}

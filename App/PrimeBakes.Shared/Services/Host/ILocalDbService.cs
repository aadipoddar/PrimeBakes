namespace PrimeBakes.Shared.Services.Host;

public interface ILocalDbService
{
	Task SyncDataBackground();

	Task InstallSqlServer();

	Task SetupDatabase();

	Task UninstallSqlServer();
}

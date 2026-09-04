namespace PrimeBakes.Shared.Services.Host;

public interface ILocalDbService
{
	Task InstallSqlServerAsync();

	Task SetupDatabaseAsync();

	Task UninstallSqlServerAsync();
}

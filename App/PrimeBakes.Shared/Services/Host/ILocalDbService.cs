namespace PrimeBakes.Shared.Services.Host;

public interface ILocalDbService
{
	Task InstallSqlServerAsync();

	Task CreateDatabaseAsync();

	Task UninstallSqlServerAsync();
}

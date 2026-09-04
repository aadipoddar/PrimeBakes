namespace PrimeBakes.Shared.Services.Host;

public interface ILocalDbService
{
	Task SetupLocalDatabaseAsync();

	Task UninstallLocalDatabaseAsync();
}

using PrimeBakes.Shared.Services.Host;

namespace PrimeBakes.Web.Services.Host;

public class LocalDbService : ILocalDbService
{
	public async Task SetupLocalDatabaseAsync() =>
		await Task.CompletedTask;

	public async Task UninstallLocalDatabaseAsync() =>
		await Task.CompletedTask;
}

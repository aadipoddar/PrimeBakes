using PrimeBakes.Shared.Services.Host;

namespace PrimeBakes.Web.Services.Host;

public class LocalDbService : ILocalDbService
{
	public async Task<bool> LocalDBAvailable() =>
		await Task.FromResult(false);

	public async Task SyncDataBackground() =>
		await Task.CompletedTask;

	public async Task InstallSqlServer() =>
		await Task.CompletedTask;

	public async Task SetupDatabase() =>
		await Task.CompletedTask;

	public async Task UninstallSqlServer() =>
		await Task.CompletedTask;
}

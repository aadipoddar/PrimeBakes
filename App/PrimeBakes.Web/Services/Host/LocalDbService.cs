using PrimeBakes.Shared.Services.Host;

namespace PrimeBakes.Web.Services.Host;

public class LocalDbService : ILocalDbService
{
	public async Task InstallSqlServerAsync() =>
		await Task.CompletedTask;

	public async Task SetupDatabaseAsync() =>
		await Task.CompletedTask;

	public async Task UninstallSqlServerAsync() =>
		await Task.CompletedTask;
}

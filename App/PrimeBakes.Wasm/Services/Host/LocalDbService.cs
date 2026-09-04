using PrimeBakes.Shared.Services.Host;

namespace PrimeBakes.Wasm.Services.Host;

public class LocalDbService : ILocalDbService
{
	public async Task InstallSqlServerAsync() =>
		await Task.CompletedTask;

	public async Task CreateDatabaseAsync() =>
		await Task.CompletedTask;

	public async Task UninstallSqlServerAsync() =>
		await Task.CompletedTask;
}

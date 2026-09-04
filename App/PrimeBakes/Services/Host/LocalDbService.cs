using PrimeBakes.Shared.Services.Host;

#if WINDOWS
using PrimeBakes.Platforms.Windows;
#endif

namespace PrimeBakes.Services.Host;

public class LocalDbService : ILocalDbService
{
	public async Task<bool> LocalDBAvailable() =>
#if WINDOWS
		await Task.FromResult(LocalDbManager.IsDatabaseReady);
#else
		await Task.FromResult(false);
#endif

	public async Task SyncDataBackground()
	{
#if WINDOWS
		await LocalDbManager.SyncDataBackground();
#endif
		await Task.CompletedTask;
	}

	public async Task InstallSqlServer()
	{
#if WINDOWS
		await LocalDbManager.InstallSqlServer();
#endif
		await Task.CompletedTask;
	}

	public async Task SetupDatabase()
	{
#if WINDOWS
		await Task.Run(LocalDbManager.SetupDatabase);
#endif
		await Task.CompletedTask;
	}

	public async Task UninstallSqlServer()
	{
#if WINDOWS
		await LocalDbManager.UninstallSqlServer();
#endif
		await Task.CompletedTask;
	}
}

using PrimeBakes.Shared.Services.Host;

#if WINDOWS
using PrimeBakes.Platforms.Windows;
#endif

namespace PrimeBakes.Services.Host;

public class LocalDbService : ILocalDbService
{
	public async Task InstallSqlServerAsync()
	{
#if WINDOWS
		await LocalDbManager.InstallSqlServer();
#endif
		await Task.CompletedTask;
	}

	public async Task CreateDatabaseAsync()
	{
#if WINDOWS
		await LocalDbManager.CreateDatabase();
#endif
		await Task.CompletedTask;
	}

	public async Task UninstallSqlServerAsync()
	{
#if WINDOWS
		await LocalDbManager.UninstallSqlServer();
#endif
		await Task.CompletedTask;
	}
}

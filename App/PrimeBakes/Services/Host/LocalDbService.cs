using PrimeBakes.Shared.Services.Host;

#if WINDOWS
using PrimeBakes.Platforms.Windows;
#endif

namespace PrimeBakes.Services.Host;

public class LocalDbService : ILocalDbService
{
	public async Task SetupLocalDatabaseAsync()
	{
#if WINDOWS
		LocalDbManager.RunSetup();
#endif
		await Task.CompletedTask;
	}

	public async Task UninstallLocalDatabaseAsync()
	{
#if WINDOWS
		await LocalDbManager.RunUninstall();
#endif
		await Task.CompletedTask;
	}
}

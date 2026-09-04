#if ANDROID
using PrimeBakes.Platforms.Android;
#elif WINDOWS
using PrimeBakes.Platforms.Windows;
#endif

using PrimeBakes.Shared.Services.Host;

namespace PrimeBakes.Services.Host;

public class UpdateService : IUpdateService
{
	public async Task UninstallAsync()
	{
#if WINDOWS
		UpdaterManager.Uninstall();
#endif
		await Task.CompletedTask;
	}

	public async Task<bool> CheckForUpdatesAsync(string githubRepoOwner, string githubRepoName, string setupFileName, string currentVersion)
	{
#if ANDROID || WINDOWS
		return await UpdaterManager.CheckForUpdates(githubRepoOwner, githubRepoName, setupFileName, currentVersion);
#else
        await Task.CompletedTask;
        return false;
#endif
	}

	public async Task UpdateAppAsync(string githubRepoOwner, string githubRepoName, string setupFileName, IProgress<int> progress = null, bool forceUpdate = false) =>
#if ANDROID || WINDOWS
		await UpdaterManager.UpdateApp(githubRepoOwner, githubRepoName, setupFileName, progress, forceUpdate);
#else
		await Task.CompletedTask;
#endif

}

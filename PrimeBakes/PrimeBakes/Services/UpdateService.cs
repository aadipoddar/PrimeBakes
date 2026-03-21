using PrimeBakes.Shared.Services;

namespace PrimeBakes.Services;

public class UpdateService : IUpdateService
{
	public async Task<bool> CheckForUpdatesAsync(string githubRepoOwner, string githubRepoName, string setupFileName, string currentVersion)
	{
#if ANDROID || WINDOWS
		return await UpdaterManager.CheckForUpdates(githubRepoOwner, githubRepoName, setupFileName, currentVersion);
#else
        await Task.CompletedTask;
        return false;
#endif
	}

	public async Task UpdateAppAsync(string githubRepoOwner, string githubRepoName, string setupFileName, IProgress<int> progress = null, bool forceUpdate = false)
	{
#if ANDROID || WINDOWS
		await UpdaterManager.UpdateApp(githubRepoOwner, githubRepoName, setupFileName, progress, forceUpdate);
#else
        await Task.CompletedTask;
#endif
	}
}

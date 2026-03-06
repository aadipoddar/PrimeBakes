using PrimeBakes.Shared.Services;

namespace PrimeBakes.Services;

public class UpdateService : IUpdateService
{
	public async Task<bool> CheckForUpdatesAsync(string githubRepoOwner, string githubRepoName, string setupFileName, string currentVersion)
	{
#if ANDROID || WINDOWS
		return await AadiSoftUpdater.CheckForUpdates(githubRepoOwner, githubRepoName, setupFileName, currentVersion);
#else
        await Task.CompletedTask;
        return false;
#endif
	}

	public async Task UpdateAppAsync(string githubRepoOwner, string githubRepoName, string setupFileName, IProgress<int> progress = null)
	{
#if ANDROID || WINDOWS
		await AadiSoftUpdater.UpdateApp(githubRepoOwner, githubRepoName, setupFileName, progress);
#else
        await Task.CompletedTask;
#endif
	}
}

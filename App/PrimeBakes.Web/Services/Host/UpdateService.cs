using PrimeBakes.Shared.Services.Host;

namespace PrimeBakes.Web.Services.Host;

public class UpdateService : IUpdateService
{
	public async Task UninstallAsync() =>
		await Task.CompletedTask;

	public async Task<bool> CheckForUpdatesAsync(string githubRepoOwner, string githubRepoName, string setupAPKName, string currentVersion)
	{
		await Task.CompletedTask;
		return false;
	}

	public async Task UpdateAppAsync(string githubRepoOwner, string githubRepoName, string setupAPKName, IProgress<int> progress = null, bool forceUpdate = false) =>
		await Task.CompletedTask;
}
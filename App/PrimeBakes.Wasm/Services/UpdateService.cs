using PrimeBakes.Shared.Services;

namespace PrimeBakes.Wasm.Services;

public class UpdateService : IUpdateService
{
	public Task<bool> CheckForUpdatesAsync(string githubRepoOwner, string githubRepoName, string setupAPKName, string currentVersion) =>
		Task.FromResult(false);

	public Task UpdateAppAsync(string githubRepoOwner, string githubRepoName, string setupAPKName, IProgress<int>? progress = null, bool forceUpdate = false) =>
		Task.CompletedTask;
}

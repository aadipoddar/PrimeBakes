using Microsoft.JSInterop;

using PrimeBakes.Shared.Services.Host;

namespace PrimeBakes.Wasm.Services.Host;

public class UpdateService(IJSRuntime jsRuntime) : IUpdateService
{
	public Task UninstallAsync() =>
		Task.CompletedTask;

	public Task<bool> CheckForUpdatesAsync(string githubRepoOwner, string githubRepoName, string setupAPKName, string currentVersion) =>
		Task.FromResult(false);

	public async Task UpdateAppAsync(string githubRepoOwner, string githubRepoName, string setupAPKName, IProgress<int>? progress = null, bool forceUpdate = false) =>
		await jsRuntime.InvokeVoidAsync("appUpdate.forceReload");
}

using Android.Content;

using System.Text.Json;

using Application = Android.App.Application;

namespace PrimeBakes.Services;

public static class AadiSoftUpdater
{
	private const string LatestVersionMarker = "Latest Version = ";

	public static async Task<bool> CheckForUpdates(string githubRepoOwner, string githubRepoName, string setupFileName, string currentVersion)
	{
		var latestVersion = await GetLatestVersionFromGithubReadme(githubRepoOwner, githubRepoName);

		if (string.IsNullOrWhiteSpace(latestVersion) || string.Equals(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
			return false;

		return await ReleaseTagHasAssets(githubRepoOwner, githubRepoName, latestVersion, setupFileName);
	}

	private static async Task<bool> ReleaseTagHasAssets(string githubRepoOwner, string githubRepoName, string tag, string setupFileName)
	{
		var cacheBuster = DateTime.UtcNow.Ticks.ToString();
		var releaseApiUrl = $"https://api.github.com/repos/{githubRepoOwner}/{githubRepoName}/releases/tags/{tag}?cb={cacheBuster}";
		var expectedAssetName = $"{setupFileName}.apk";

		using var client = CreateHttpClient(withUserAgent: true);

		using var response = await client.GetAsync(releaseApiUrl);
		if (!response.IsSuccessStatusCode)
			return false;

		var content = await response.Content.ReadAsStringAsync();
		using var document = JsonDocument.Parse(content);

		if (!document.RootElement.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
			return false;

		foreach (var asset in assets.EnumerateArray())
		{
			if (!asset.TryGetProperty("name", out var nameElement))
				continue;

			var assetName = nameElement.GetString();
			if (string.Equals(assetName, expectedAssetName, StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	private static async Task<string> GetLatestVersionFromGithubReadme(string githubRepoOwner, string githubRepoName)
	{
		var fileUrl = $"https://raw.githubusercontent.com/{githubRepoOwner}/{githubRepoName}/refs/heads/main/README.md";
		var cacheBuster = DateTime.UtcNow.Ticks.ToString();
		var requestUrl = $"{fileUrl}?cb={cacheBuster}";
		using var client = CreateHttpClient();
		var fileContent = await client.GetStringAsync(requestUrl);

		if (!fileContent.Contains(LatestVersionMarker, StringComparison.Ordinal))
			return string.Empty;

		return fileContent.Substring(fileContent.IndexOf(LatestVersionMarker, StringComparison.Ordinal) + LatestVersionMarker.Length, 7);
	}

	public static async Task UpdateApp(string githubRepoOwner, string githubRepoName, string setupAPKName, IProgress<int> progress = null)
	{
		var latestVersion = await GetLatestVersionFromGithubReadme(githubRepoOwner, githubRepoName);
		if (string.IsNullOrWhiteSpace(latestVersion))
			throw new Exception("Latest Version not found in README.");

		var url = $"https://github.com/{githubRepoOwner}/{githubRepoName}/releases/download/{latestVersion}/{setupAPKName}.apk";
		var filePath = Path.Combine(Application.Context.GetExternalFilesDir(null).AbsolutePath, $"{setupAPKName}.apk");

		using var client = CreateHttpClient();
		using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();

		var totalBytes = response.Content.Headers.ContentLength ?? 0;
		var downloadedBytes = 0L;

		await using var stream = await response.Content.ReadAsStreamAsync();
		await using var fileStream = new FileStream(filePath, FileMode.Create);

		var buffer = new byte[8192];
		int bytesRead;

		while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
		{
			await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
			downloadedBytes += bytesRead;

			if (totalBytes > 0 && progress != null)
			{
				var percentage = (int)(downloadedBytes * 100 / totalBytes);
				progress.Report(percentage);
			}
		}

		InstallApk(filePath);
	}

	private static void InstallApk(string filePath)
	{
		var file = new Java.IO.File(filePath);
		var fileUri = AndroidX.Core.Content.FileProvider.GetUriForFile(Application.Context, Application.Context.PackageName + ".provider", file);
		var intent = new Intent(Intent.ActionView);
		intent.SetData(fileUri);
		intent.AddFlags(ActivityFlags.NewTask);
		intent.AddFlags(ActivityFlags.GrantReadUriPermission);
		Application.Context.StartActivity(intent);
	}

	private static HttpClient CreateHttpClient(bool withUserAgent = false)
	{
		var client = new HttpClient();
		client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
		{
			NoCache = true,
			NoStore = true,
			MustRevalidate = true
		};
		client.DefaultRequestHeaders.Pragma.ParseAdd("no-cache");
		if (withUserAgent)
			client.DefaultRequestHeaders.UserAgent.ParseAdd("PrimeBakes-Updater");
		return client;
	}
}
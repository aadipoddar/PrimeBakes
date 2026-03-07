using System.Diagnostics;
using System.Text.Json;

namespace PrimeBakes.Services;

public static class AadiSoftUpdater
{
    private const string LatestVersionMarker = "Latest Version = ";

    public static async Task<bool> CheckForUpdates(string githubRepoOwner, string githubRepoName, string setupFileName, string currentVersion)
    {
        try
        {
            var latestVersion = await GetLatestVersionFromGithubReadme(githubRepoOwner, githubRepoName);
            if (string.IsNullOrWhiteSpace(latestVersion) || string.Equals(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
                return false;

            return await ReleaseTagHasAssets(githubRepoOwner, githubRepoName, latestVersion, setupFileName);
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> ReleaseTagHasAssets(string githubRepoOwner, string githubRepoName, string tag, string setupFileName)
    {
        var cacheBuster = DateTime.UtcNow.Ticks.ToString();
        var releaseApiUrl = $"https://api.github.com/repos/{githubRepoOwner}/{githubRepoName}/releases/tags/{tag}?cb={cacheBuster}";
        var expectedAssetName = $"{setupFileName}.zip";

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

    public static async Task UpdateApp(string githubRepoOwner, string githubRepoName, string zipFileName, IProgress<int> progress = null, bool forceUpdate = false)
    {
        var url = forceUpdate
            ? $"https://github.com/{githubRepoOwner}/{githubRepoName}/releases/latest/download/{zipFileName}.zip"
            : string.Empty;

        if (!forceUpdate)
        {
            var latestVersion = await GetLatestVersionFromGithubReadme(githubRepoOwner, githubRepoName);
            if (string.IsNullOrWhiteSpace(latestVersion))
                throw new Exception("Latest Version not found in README.");

            url = $"https://github.com/{githubRepoOwner}/{githubRepoName}/releases/download/{latestVersion}/{zipFileName}.zip";
        }

        var zipPath = Path.Combine(Path.GetTempPath(), $"{zipFileName}.zip");
        var extractPath = Path.Combine(Path.GetTempPath(), $"{zipFileName}_update");
        var appPath = AppContext.BaseDirectory;

        // Download ZIP
        using var client = CreateHttpClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var downloadedBytes = 0L;

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(zipPath, FileMode.Create);

        var buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            downloadedBytes += bytesRead;

            if (totalBytes > 0 && progress != null)
            {
                var percentage = (int)(downloadedBytes * 100 / totalBytes);
                progress.Report(percentage);
            }
        }

        fileStream.Close();

        // Run batch script to extract and replace files after app closes
        RunUpdateScript(zipPath, extractPath, appPath);
    }

    private static void RunUpdateScript(string zipPath, string extractPath, string appPath)
    {
        var batchFilePath = Path.Combine(Path.GetTempPath(), "primebakes_update.bat");
        var exePath = Path.Combine(appPath, "PrimeBakes.exe");

        var batchScript = $@"
@echo off
echo Updating Prime Bakes...
echo.
timeout /t 2 /nobreak >nul
echo Extracting update...
if exist ""{extractPath}"" rmdir /s /q ""{extractPath}""
powershell -Command ""Expand-Archive -Path '{zipPath}' -DestinationPath '{extractPath}' -Force""
echo.
echo Copying new files...
xcopy /s /y /q ""{extractPath}\*"" ""{appPath}""
echo.
echo Cleaning up...
rmdir /s /q ""{extractPath}""
del ""{zipPath}""
echo.
echo Update complete! Starting app...
start """" ""{exePath}""
del ""%~f0""
";

        File.WriteAllText(batchFilePath, batchScript);

        var startInfo = new ProcessStartInfo
        {
            FileName = batchFilePath,
            UseShellExecute = true,
            CreateNoWindow = false
        };

        Process.Start(startInfo);

        // Exit the current application to allow update
        Environment.Exit(0);
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

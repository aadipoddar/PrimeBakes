using PrimeBakes.Shared.Services;

namespace PrimeBakes.Services;

public class DataStorageService : IDataStorageService
{
	public async Task SecureSaveAsync(string key, string value) =>
		await SecureStorage.Default.SetAsync(key, value);

	public async Task<string?> SecureGetAsync(string key) =>
		await SecureStorage.Default.GetAsync(key);

	public async Task SecureRemove(string key) =>
		SecureStorage.Default.Remove(key);

	public Task SecureRemoveAll()
	{
		SecureStorage.Default.RemoveAll();

		foreach (var file in Directory.GetFiles(FileSystem.Current.AppDataDirectory, "*.json"))
		{
			try { File.Delete(file); }
			catch { }
		}

		return Task.CompletedTask;
	}


	public async Task<bool> LocalExists(string key) =>
		File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, key));

	public async Task LocalSaveAsync(string key, string value) =>
		await File.WriteAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, key), value);

	public async Task<string?> LocalGetAsync(string key)
	{
		if (File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, key)))
			return await File.ReadAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, key));

		return null;
	}

	public async Task LocalRemove(string key) =>
		File.Delete(Path.Combine(FileSystem.Current.AppDataDirectory, key));
}

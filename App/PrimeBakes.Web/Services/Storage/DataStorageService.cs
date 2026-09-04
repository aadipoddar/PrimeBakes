using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services;

namespace PrimeBakes.Web.Services;

public class DataStorageService(ProtectedLocalStorage protectedLocalStorage, IJSRuntime jsRuntime) : IDataStorageService
{
	private readonly ProtectedLocalStorage _protectedLocalStorage = protectedLocalStorage;
	private readonly IJSRuntime _jsRuntime = jsRuntime;

	public async Task SecureSaveAsync(string key, string value)
	{
		try { await _protectedLocalStorage.SetAsync(key, value); }
		catch { }
	}

	public async Task<string?> SecureGetAsync(string key) =>
		(await _protectedLocalStorage.GetAsync<string>(key)).Value;

	public async Task SecureRemove(string key)
	{
		try { await _protectedLocalStorage.DeleteAsync(key); }
		catch { }
	}

	public async Task SecureRemoveAll()
	{
		try { await _jsRuntime.InvokeVoidAsync("localStorage.clear"); }
		catch { }
	}


	public async Task<bool> LocalExists(string key) =>
		(await _protectedLocalStorage.GetAsync<string>(key)).Success;

	public async Task LocalSaveAsync(string key, string value) =>
		await SecureSaveAsync(key, value);

	public async Task<string?> LocalGetAsync(string key) =>
		await SecureGetAsync(key);

	public async Task LocalRemove(string key) =>
		await SecureRemove(key);
}

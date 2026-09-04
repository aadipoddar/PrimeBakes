using Microsoft.JSInterop;

using PrimeBakes.Shared.Services.Storage;

namespace PrimeBakes.Wasm.Services.Storage;

public class DataStorageService(IJSRuntime jsRuntime) : IDataStorageService
{
	private readonly IJSRuntime _jsRuntime = jsRuntime;

	public async Task SecureSaveAsync(string key, string value)
	{
		try { await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value); }
		catch { }
	}

	public async Task<string?> SecureGetAsync(string key)
	{
		try { return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key); }
		catch { return null; }
	}

	public async Task SecureRemove(string key)
	{
		try { await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key); }
		catch { }
	}

	public async Task SecureRemoveAll()
	{
		try { await _jsRuntime.InvokeVoidAsync("localStorage.clear"); }
		catch { }
	}

	public async Task<bool> LocalExists(string key) =>
		await SecureGetAsync(key) is not null;

	public async Task LocalSaveAsync(string key, string value) =>
		await SecureSaveAsync(key, value);

	public async Task<string?> LocalGetAsync(string key) =>
		await SecureGetAsync(key);

	public async Task LocalRemove(string key) =>
		await SecureRemove(key);
}

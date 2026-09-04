using Microsoft.JSInterop;

using PrimeBakes.Shared.Services.Storage;

namespace PrimeBakes.Wasm.Services.Storage;

public class SaveAndViewService(IJSRuntime jsRuntime) : ISaveAndViewService
{
	private readonly IJSRuntime _jsRuntime = jsRuntime;

	public async Task<string> SaveAndView(string fileName, MemoryStream stream)
	{
		await _jsRuntime.InvokeVoidAsync("saveFile", Convert.ToBase64String(stream.ToArray()), fileName);
		return fileName;
	}
}

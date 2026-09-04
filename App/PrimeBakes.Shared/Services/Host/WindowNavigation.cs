using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services.Device;

namespace PrimeBakes.Shared.Services.Host;

public class WindowNavigation(IFormFactor formFactor, IJSRuntime jsRuntime, NavigationManager navigationManager)
{
	public static Func<string, bool> OpenRouteInNewWindow { get; set; }
	public static Func<bool> CloseCurrentWindow { get; set; }

	public async Task NavigateToRoute(string route)
	{
		if (formFactor.GetFormFactor() is "Web" or "Wasm")
			await jsRuntime.InvokeVoidAsync("open", route, "_blank");
		else if (OpenRouteInNewWindow is not null && OpenRouteInNewWindow(route))
			return;
		else
			navigationManager.NavigateTo(route);
	}

	public async Task CloseWindowOrTab()
	{
		if (formFactor.GetFormFactor() is "Web" or "Wasm")
			await jsRuntime.InvokeVoidAsync("pageCloseGuard.close");
		else
			CloseCurrentWindow?.Invoke();
	}
}

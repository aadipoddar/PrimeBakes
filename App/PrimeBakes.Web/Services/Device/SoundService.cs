using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using PrimeBakes.Shared.Services.Device;

namespace PrimeBakes.Web.Services.Device;

public class SoundService(IJSRuntime jsRuntime) : ISoundService
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = jsRuntime;

    public async Task PlaySound(string soundFileName) =>
        await JSRuntime.InvokeVoidAsync("PlaySound", soundFileName);
}

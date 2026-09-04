using Plugin.Maui.Audio;

using PrimeBakes.Shared.Services.Device;

namespace PrimeBakes.Services.Device;

public class SoundService : ISoundService
{
    public async Task PlaySound(string soundFileName) =>
        AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync(soundFileName)).Play();
}

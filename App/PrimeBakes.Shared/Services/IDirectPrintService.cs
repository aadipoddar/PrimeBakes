namespace PrimeBakes.Shared.Services;

/// <summary>
/// Abstraction for silent raw-byte printing (no dialog).
/// On Windows, sends ESC/POS data directly to the default printer via the spooler.
/// On other platforms, <see cref="IsSupported"/> returns false and callers fall back
/// to the browser print dialog.
/// </summary>
public interface IDirectPrintService
{
	/// <summary>Whether this platform can print raw bytes silently.</summary>
	bool IsSupported { get; }

	/// <summary>Sends raw ESC/POS bytes to the default printer without showing a dialog.</summary>
	Task PrintRawAsync(byte[] data);
}

/// <summary>No-op implementation for platforms that don't support direct printing.</summary>
public class NullDirectPrintService : IDirectPrintService
{
	public bool IsSupported => false;
	public Task PrintRawAsync(byte[] data) => Task.CompletedTask;
}

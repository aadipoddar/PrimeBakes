namespace PrimeBakes.Shared.Services.Host;

public sealed class PageRefreshState
{
	public event Action Requested;

	public void Request() => Requested?.Invoke();
}

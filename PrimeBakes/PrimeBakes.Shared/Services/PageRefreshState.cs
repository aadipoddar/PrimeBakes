namespace PrimeBakes.Shared.Services;

public sealed class PageRefreshState
{
	public event Action Requested;

	public void Request() => Requested?.Invoke();
}

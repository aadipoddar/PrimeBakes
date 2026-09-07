namespace PrimeBakes.Models.DataAccess;

public static class OfflineState
{
	public static bool Offline { get; set; }

	public static bool LocalDBAvailable { get; set; }

	public static int TerminalNo { get; set; }

	private static bool _syncing;

	public static bool Syncing
	{
		get => _syncing;
		set
		{
			_syncing = value;
			SyncingChanged?.Invoke();
		}
	}

	public static event Action SyncingChanged;
}

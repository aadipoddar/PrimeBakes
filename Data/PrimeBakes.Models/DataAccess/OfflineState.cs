namespace PrimeBakes.Models.DataAccess;

public static class OfflineState
{
	public static bool Offline { get; set; }

	public static bool LocalDBAvailable { get; set; }

	public static int TerminalNo { get; set; }
}

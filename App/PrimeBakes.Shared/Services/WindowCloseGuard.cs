namespace PrimeBakes.Shared.Services;

public static class WindowCloseGuard
{
	private static object _owner;

	public static bool BlockClose { get; private set; }
	public static string Message { get; private set; }

	public static void Set(object owner, bool block, string message)
	{
		if (block)
		{
			_owner = owner;
			BlockClose = true;
			if (!string.IsNullOrWhiteSpace(message))
				Message = message;
		}
		else if (ReferenceEquals(_owner, owner))
		{
			_owner = null;
			BlockClose = false;
		}
	}

	public static void Clear(object owner)
	{
		if (ReferenceEquals(_owner, owner))
		{
			_owner = null;
			BlockClose = false;
		}
	}
}

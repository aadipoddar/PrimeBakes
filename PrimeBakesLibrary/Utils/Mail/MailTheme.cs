namespace PrimeBakesLibrary.Utils.Mail;

public static class MailTheme
{
	/// <summary>
	/// Solid, email-safe equivalent of the app.css <c>--app-bg</c> gradient
	/// (<c>linear-gradient(135deg, #ffeef5 0%, #fff5f9 100%)</c>). Gradients
	/// are unreliable on <c>body</c>/<c>table</c> across mail clients, so this
	/// is the visual midpoint of the two gradient stops.
	/// </summary>
	public const string PageBackground = "#fff2f7";

	/// <summary>Deeper pink gradient stop — used for inset boxes (e.g. the code box).</summary>
	public const string SurfaceTint = "#ffeef5";

	/// <summary>Border tone that matches the pink <see cref="SurfaceTint"/>.</summary>
	public const string SurfaceBorder = "#f3cfe0";
}

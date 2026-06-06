namespace PrimeBakesLibrary.Utils.Mail;

public static class MailTheme
{
	/// <summary>
	/// Solid, email-safe equivalent of the app.css <c>--app-bg</c> gradient
	/// (<c>linear-gradient(135deg, #fdf6ee 0%, #f6e7d8 100%)</c>). Gradients
	/// are unreliable on <c>body</c>/<c>table</c> across mail clients, so this
	/// is the visual midpoint of the two gradient stops.
	/// </summary>
	public const string PageBackground = "#faefe3";

	/// <summary>Warmer gradient stop — used for inset boxes (e.g. the code box).</summary>
	public const string SurfaceTint = "#f6e7d8";

	/// <summary>Border tone that matches the warm <see cref="SurfaceTint"/>.</summary>
	public const string SurfaceBorder = "#e3cdb5";
}

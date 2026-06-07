using Microsoft.AspNetCore.Components;

using PrimeBakesLibrary.DataAccess;

using System.Reflection;

namespace PrimeBakes.Shared.Components.Page;

public partial class Footer
{
	[Parameter]
	public bool ShowVersion { get; set; } = true;

	private string Factor =>
		FormFactor.GetFormFactor();

	private string Platform =>
		FormFactor.GetPlatform();

	private static string AppVersion =>
		Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

	private static string CopyrightUrl =>
		Secrets.AadiSoftWebsite;
}
